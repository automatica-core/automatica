import { fakeAsync, tick } from "@angular/core/testing";
import { CameraPlayerState, CameraStreamPlayer, cameraWebSocketUrl, mediaSourceClass, MSE_CODECS, supportedCodecs } from "./camera-stream-player";

class FakeSourceBuffer extends EventTarget {
  mode = "";
  updating = false;
  appended: ArrayBuffer[] = [];
  buffered = { length: 0, start: (_: number) => 0, end: (_: number) => 0 };

  appendBuffer(data: ArrayBuffer) {
    this.appended.push(data);
    this.updating = true;
    // the browser finishes asynchronously
    setTimeout(() => {
      this.updating = false;
      this.dispatchEvent(new Event("updateend"));
    });
  }

  remove() { }
}

class FakeMediaSource extends EventTarget {
  static instances: FakeMediaSource[] = [];
  static supported = (t: string) => t.includes("avc1") || t.includes("mp4a.40.2");
  static isTypeSupported(type: string) {
    return FakeMediaSource.supported(type);
  }

  readyState = "closed";
  buffers: FakeSourceBuffer[] = [];
  types: string[] = [];

  constructor() {
    super();
    FakeMediaSource.instances.push(this);
  }

  open() {
    this.readyState = "open";
    this.dispatchEvent(new Event("sourceopen"));
  }

  addSourceBuffer(type: string) {
    this.types.push(type);
    const sb = new FakeSourceBuffer();
    this.buffers.push(sb);
    return sb as any;
  }

  endOfStream() { }
}

class FakeWebSocket {
  static instances: FakeWebSocket[] = [];
  binaryType = "";
  sent: string[] = [];
  closed = false;
  onopen: () => void;
  onmessage: (e: { data: any }) => void;
  onclose: () => void;

  constructor(public url: string) {
    FakeWebSocket.instances.push(this);
  }

  send(data: string) {
    this.sent.push(data);
  }

  close() {
    this.closed = true;
  }
}

function fakeVideo() {
  return {
    paused: true,
    currentTime: 0,
    src: "",
    srcObject: null,
    play: jasmine.createSpy("play").and.callFake(function () { this.paused = false; return Promise.resolve(); }),
    pause: jasmine.createSpy("pause"),
    load: jasmine.createSpy("load"),
    removeAttribute: jasmine.createSpy("removeAttribute")
  } as any;
}

describe("camera stream player", () => {

  beforeEach(() => {
    FakeMediaSource.instances = [];
    FakeWebSocket.instances = [];
  });

  it("asks go2rtc only for the codecs the browser plays", () => {
    const codecs = supportedCodecs(t => t.includes("avc1.640029") || t.includes("opus"));

    expect(codecs).toBe("avc1.640029,opus");
    expect(MSE_CODECS[0]).toBe("avc1.640029");
  });

  it("treats a throwing isTypeSupported as unsupported", () => {
    expect(supportedCodecs(() => { throw new Error("nope"); })).toBe("");
  });

  it("builds the WebSocket URL of the camera endpoint with the token", () => {
    const location = { origin: "https://home.example" } as Location;

    expect(cameraWebSocketUrl("abc", "sub", "t k", "/webapi", location)).toBe("wss://home.example/webapi/camera/abc/ws?stream=sub&access_token=t%20k");
    expect(cameraWebSocketUrl("abc", "main", "tok", "http://192.168.1.5:5001/webapi", location)).toBe("ws://192.168.1.5:5001/webapi/camera/abc/ws?stream=main&access_token=tok");
  });

  it("prefers ManagedMediaSource (iOS) over MediaSource", () => {
    const managed = class { };
    const plain = class { };

    expect(mediaSourceClass({ ManagedMediaSource: managed, MediaSource: plain }) as any).toBe(managed);
    expect(mediaSourceClass({ MediaSource: plain }) as any).toBe(plain);
    expect(mediaSourceClass({})).toBeUndefined();
    expect(CameraStreamPlayer.isSupported({ WebSocket: FakeWebSocket })).toBeFalse();
  });

  it("requests MSE, creates the source buffer from the answer and appends the fragments", fakeAsync(() => {
    const states: CameraPlayerState[] = [];
    const video = fakeVideo();
    const win = { MediaSource: FakeMediaSource, WebSocket: FakeWebSocket };
    spyOn(URL, "createObjectURL").and.returnValue("blob:ms");
    spyOn(URL, "revokeObjectURL");
    const player = new CameraStreamPlayer(video, () => "ws://x/webapi/camera/1/ws?stream=sub", s => states.push(s), win);

    player.start();
    const ms = FakeMediaSource.instances[0];
    expect(video.src).toBe("blob:ms");
    ms.open();

    const ws = FakeWebSocket.instances[0];
    expect(ws.url).toBe("ws://x/webapi/camera/1/ws?stream=sub");
    expect(ws.binaryType).toBe("arraybuffer");
    ws.onopen();
    const request = JSON.parse(ws.sent[0]);
    expect(request.type).toBe("mse");
    expect(request.value).toBe("avc1.640029,avc1.64002A,avc1.640033,mp4a.40.2");

    ws.onmessage({ data: JSON.stringify({ type: "mse", value: "video/mp4; codecs=\"avc1.64001F\"" }) });
    expect(ms.types).toEqual(["video/mp4; codecs=\"avc1.64001F\""]);

    const init = new ArrayBuffer(10);
    const fragment = new ArrayBuffer(20);
    ws.onmessage({ data: init });
    ws.onmessage({ data: fragment });
    // the second fragment waits until the buffer is done with the first
    expect(ms.buffers[0].appended).toEqual([init]);
    tick(1);
    expect(ms.buffers[0].appended).toEqual([init, fragment]);
    expect(ms.buffers[0].mode).toBe("segments");
    expect(video.play).toHaveBeenCalled();
    expect(states).toContain("playing");

    player.stop();
    expect(ws.closed).toBeTrue();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:ms");
    expect(states[states.length - 1]).toBe("idle");
    tick(60000);
    expect(FakeWebSocket.instances.length).toBe(1);
  }));

  it("reconnects with back-off when the socket closes, with a fresh URL (token)", fakeAsync(() => {
    const states: CameraPlayerState[] = [];
    let n = 0;
    const win = { MediaSource: FakeMediaSource, WebSocket: FakeWebSocket };
    spyOn(URL, "createObjectURL").and.returnValue("blob:ms");
    spyOn(URL, "revokeObjectURL");
    const player = new CameraStreamPlayer(fakeVideo(), () => `ws://x/ws?access_token=${++n}`, s => states.push(s), win);

    player.start();
    FakeMediaSource.instances[0].open();
    FakeWebSocket.instances[0].onclose();

    expect(states[states.length - 1]).toBe("connecting");
    tick(1999);
    expect(FakeMediaSource.instances.length).toBe(1);
    tick(1);
    FakeMediaSource.instances[1].open();
    expect(FakeWebSocket.instances[1].url).toBe("ws://x/ws?access_token=2");

    // a second failure in a row shows the error
    FakeWebSocket.instances[1].onclose();
    expect(states[states.length - 1]).toBe("error");
    player.stop();
    tick(60000);
  }));

  it("shows the go2rtc error and retries", fakeAsync(() => {
    const states: [CameraPlayerState, string][] = [];
    const win = { MediaSource: FakeMediaSource, WebSocket: FakeWebSocket };
    spyOn(URL, "createObjectURL").and.returnValue("blob:ms");
    spyOn(URL, "revokeObjectURL");
    const player = new CameraStreamPlayer(fakeVideo(), () => "ws://x", (s, d) => states.push([s, d]), win);

    player.start();
    FakeMediaSource.instances[0].open();
    FakeWebSocket.instances[0].onmessage({ data: JSON.stringify({ type: "error", value: "streams: dial tcp 192.168.1.20:554: i/o timeout" }) });

    expect(states).toContain(["error", "streams: dial tcp 192.168.1.20:554: i/o timeout"]);
    expect(FakeWebSocket.instances[0].closed).toBeTrue();
    player.stop();
    tick(60000);
  }));

  it("uses srcObject with ManagedMediaSource", () => {
    const video = fakeVideo();
    const win = { ManagedMediaSource: FakeMediaSource, WebSocket: FakeWebSocket };
    const player = new CameraStreamPlayer(video, () => "ws://x", () => { }, win);

    player.start();

    expect(video.srcObject).toBe(FakeMediaSource.instances[0]);
    expect(video.disableRemotePlayback).toBeTrue();
    player.stop();
  });

  it("reports unsupported without MediaSource", () => {
    const states: CameraPlayerState[] = [];
    const player = new CameraStreamPlayer(fakeVideo(), () => "ws://x", s => states.push(s), { WebSocket: FakeWebSocket });

    player.start();

    expect(states).toEqual(["unsupported"]);
    expect(FakeWebSocket.instances.length).toBe(0);
  });
});
