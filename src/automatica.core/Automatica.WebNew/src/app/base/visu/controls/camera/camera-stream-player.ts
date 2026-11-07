import { BaseServiceHelper } from "src/app/services/base-server-helper";

/** what the player is doing, shown by the camera tile */
export type CameraPlayerState = "idle" | "connecting" | "playing" | "error" | "unsupported";

/** codecs go2rtc can deliver over MSE, in its own order of preference (www/video-rtc.js) */
export const MSE_CODECS = [
  "avc1.640029",
  "avc1.64002A",
  "avc1.640033",
  "hvc1.1.6.L153.B0",
  "mp4a.40.2",
  "mp4a.40.5",
  "flac",
  "opus"
];

/** keep this much video behind the playhead, older data is removed from the buffer */
const KEEP_BEHIND_S = 10;
/** further behind the live edge than this: jump forward */
const MAX_LATENCY_S = 2.5;
const MAX_QUEUE_BYTES = 4 * 1024 * 1024;

interface MediaSourceLike extends EventTarget {
  readyState: string;
  addSourceBuffer(type: string): SourceBuffer;
  endOfStream(): void;
}

/** the MediaSource implementation of this browser: ManagedMediaSource (iOS 17.1+, Safari 17+) or MediaSource */
export function mediaSourceClass(win: any = window): { new(): MediaSourceLike; isTypeSupported(type: string): boolean; managed: boolean } | undefined {
  if (win.ManagedMediaSource) {
    const cls = win.ManagedMediaSource;
    cls.managed = true;
    return cls;
  }
  if (win.MediaSource) {
    const cls = win.MediaSource;
    cls.managed = false;
    return cls;
  }
  return void 0;
}

/** the codec list for go2rtc's {"type":"mse"} request: the candidates this browser can play */
export function supportedCodecs(isTypeSupported: (type: string) => boolean): string {
  return MSE_CODECS.filter(c => {
    try {
      return isTypeSupported(`video/mp4; codecs="${c}"`);
    } catch {
      return false;
    }
  }).join(",");
}

/** webapi/camera/{id}/ws as ws(s):// URL with the token (browsers cannot set headers on WebSockets) */
export function cameraWebSocketUrl(cameraId: string, quality: "main" | "sub", token: string, base = BaseServiceHelper.getApiBaseUrl(), location: Location = window.location): string {
  let http = base;
  if (!/^https?:\/\//i.test(http)) {
    http = location.origin + (http.startsWith("/") ? "" : "/") + http;
  }
  const ws = http.replace(/^http/i, "ws");
  return `${ws}/camera/${encodeURIComponent(cameraId)}/ws?stream=${quality}&access_token=${encodeURIComponent(token ?? "")}`;
}

/**
 * Plays the go2rtc MSE stream of a camera in a video element: sends {"type":"mse","value":<codecs>}, gets
 * {"type":"mse","value":<mime type>} back, then appends the binary fMP4 fragments to the source buffer.
 * Reconnects with back-off until stop() is called.
 */
export class CameraStreamPlayer {

  private ws: WebSocket;
  private mediaSource: MediaSourceLike;
  private sourceBuffer: SourceBuffer;
  private objectUrl: string;
  private queue: ArrayBuffer[] = [];
  private queuedBytes = 0;
  private stopped = true;
  private retries = 0;
  private retryTimer: any;

  constructor(
    private readonly video: HTMLVideoElement,
    private readonly url: () => string,
    private readonly onState: (state: CameraPlayerState, detail?: string) => void,
    private readonly win: any = window) {
  }

  public static isSupported(win: any = window): boolean {
    return !!mediaSourceClass(win) && typeof win.WebSocket === "function";
  }

  public start() {
    if (!this.stopped) {
      return;
    }
    this.stopped = false;
    this.retries = 0;
    this.connect();
  }

  public stop() {
    this.stopped = true;
    clearTimeout(this.retryTimer);
    this.teardown();
    this.onState("idle");
  }

  private connect() {
    const cls = mediaSourceClass(this.win);
    if (!cls) {
      this.onState("unsupported");
      return;
    }

    this.onState("connecting");
    this.teardown();

    const ms = new cls();
    this.mediaSource = ms;
    ms.addEventListener("sourceopen", () => this.openSocket(cls), { once: true });

    if (cls.managed) {
      // ManagedMediaSource plays only with remote playback (AirPlay) disabled
      (this.video as any).disableRemotePlayback = true;
      (this.video as any).srcObject = ms;
    } else {
      this.objectUrl = URL.createObjectURL(ms as any);
      this.video.src = this.objectUrl;
    }
  }

  private openSocket(cls: { isTypeSupported(type: string): boolean }) {
    if (this.stopped) {
      return;
    }

    let ws: WebSocket;
    try {
      ws = new this.win.WebSocket(this.url());
    } catch (e) {
      this.scheduleReconnect(String(e));
      return;
    }
    this.ws = ws;
    ws.binaryType = "arraybuffer";

    ws.onopen = () => ws.send(JSON.stringify({ type: "mse", value: supportedCodecs(t => cls.isTypeSupported(t)) }));
    ws.onmessage = (ev: MessageEvent) => {
      if (typeof ev.data === "string") {
        this.onText(ev.data);
      } else {
        this.onData(ev.data as ArrayBuffer);
      }
    };
    ws.onclose = () => {
      if (this.ws === ws) {
        this.scheduleReconnect();
      }
    };
  }

  private onText(text: string) {
    let msg: { type: string; value: string };
    try {
      msg = JSON.parse(text);
    } catch {
      return;
    }

    if (msg.type === "mse" && !this.sourceBuffer && this.mediaSource?.readyState === "open") {
      try {
        const sb = this.mediaSource.addSourceBuffer(msg.value);
        sb.mode = "segments";
        sb.addEventListener("updateend", () => this.flush());
        this.sourceBuffer = sb;
      } catch (e) {
        this.fail(`codec ${msg.value}: ${e}`);
      }
    } else if (msg.type === "error") {
      this.fail(msg.value);
    }
  }

  private onData(data: ArrayBuffer) {
    if (!this.sourceBuffer) {
      return;
    }
    if (this.queuedBytes + data.byteLength > MAX_QUEUE_BYTES) {
      // the browser cannot keep up (background tab): drop and restart at the live edge
      this.scheduleReconnect("buffer overflow");
      return;
    }
    this.queue.push(data);
    this.queuedBytes += data.byteLength;
    this.flush();
  }

  private flush() {
    const sb = this.sourceBuffer;
    if (!sb || sb.updating) {
      return;
    }

    if (this.queue.length === 0) {
      this.trim(sb);
      return;
    }

    const chunk = this.queue.shift();
    this.queuedBytes -= chunk.byteLength;
    try {
      sb.appendBuffer(chunk);
    } catch (e) {
      // QuotaExceeded: the buffer is full, start over
      this.scheduleReconnect(String(e));
      return;
    }

    if (this.video.paused) {
      this.video.play().catch(() => { /* autoplay needs muted, which the tile is */ });
    }
    this.retries = 0;
    this.onState("playing");
  }

  /** stay at the live edge and keep the buffer small */
  private trim(sb: SourceBuffer) {
    const buffered = sb.buffered;
    if (buffered.length === 0) {
      return;
    }
    const start = buffered.start(0);
    const end = buffered.end(buffered.length - 1);
    const now = this.video.currentTime;

    if (end - now > MAX_LATENCY_S) {
      this.video.currentTime = end - 0.5;
    } else if (now - start > KEEP_BEHIND_S * 2) {
      try {
        sb.remove(start, now - KEEP_BEHIND_S);
      } catch {
        // removal while updating, next round
      }
    }
  }

  private fail(detail: string) {
    this.onState("error", detail);
    this.scheduleReconnect(detail);
  }

  private scheduleReconnect(detail?: string) {
    this.teardown();
    if (this.stopped) {
      return;
    }
    this.retries++;
    this.onState(this.retries > 1 ? "error" : "connecting", detail);
    const delay = Math.min(30000, 1000 * Math.pow(2, Math.min(this.retries, 5)));
    clearTimeout(this.retryTimer);
    this.retryTimer = setTimeout(() => this.connect(), delay);
  }

  private teardown() {
    const ws = this.ws;
    this.ws = void 0;
    if (ws) {
      ws.onopen = ws.onmessage = ws.onclose = null;
      try {
        ws.close();
      } catch {
        // already closed
      }
    }

    this.queue = [];
    this.queuedBytes = 0;
    this.sourceBuffer = void 0;
    this.mediaSource = void 0;

    try {
      this.video.pause();
      (this.video as any).srcObject = null;
      this.video.removeAttribute("src");
      this.video.load();
    } catch {
      // element already gone
    }
    if (this.objectUrl) {
      URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = void 0;
    }
  }
}
