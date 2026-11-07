import { Component, OnInit, OnDestroy, AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, ElementRef, ViewChild, inject, NgZone } from "@angular/core";
import { BaseMobileComponent } from "../../base-mobile-component";
import { BaseServiceHelper } from "src/app/services/base-server-helper";
import { CameraPlayerState, CameraStreamPlayer, cameraWebSocketUrl } from "./camera-stream-player";

export enum CameraStreamQuality {
  Sub = 0,
  Main = 1
}

export enum CameraMode {
  Live = 0,
  Snapshot = 1
}

export enum CameraFit {
  Cover = 0,
  Contain = 1
}

/** snapshots are refreshed at most this often, whatever the property says */
const MIN_REFRESH_S = 2;

/**
 * Live view of a camera of the camera driver. The stream comes from go2rtc through webapi/camera/{id}:
 * the tile plays the sub stream (or a snapshot every n seconds), the fullscreen view the main stream.
 * Streams only run while the tile is on screen and the page is visible.
 */
@Component({
    selector: "visu-camera",
    templateUrl: "./camera.component.html",
    styleUrls: ["./camera.component.scss"],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class CameraComponent extends BaseMobileComponent implements OnInit, AfterViewInit, OnDestroy {

  @ViewChild("tileVideo") tileVideo: ElementRef<HTMLVideoElement>;
  @ViewChild("tile") tileElement: ElementRef<HTMLElement>;

  private readonly zone = inject(NgZone);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly host = inject(ElementRef);

  state: CameraPlayerState = "idle";
  stateDetail: string;
  snapshotUrl: string;
  snapshotFailed = false;
  fullscreenVisible = false;
  fullscreenState: CameraPlayerState = "idle";

  private player: CameraStreamPlayer;
  private fullscreenPlayer: CameraStreamPlayer;
  private observer: IntersectionObserver;
  private onScreen = false;
  private snapshotTimer: any;
  private snapshotObjectUrl: string;
  private visibilityHandler = () => this.updateStreaming();

  public get cameraId(): string {
    return this.getProperty("nodeInstance")?.Value;
  }

  public get mode(): CameraMode {
    const mode = Number(this.getPropertyValue("camera_mode"));
    if (mode === CameraMode.Snapshot || !CameraStreamPlayer.isSupported()) {
      return CameraMode.Snapshot;
    }
    return CameraMode.Live;
  }

  public get quality(): "main" | "sub" {
    return Number(this.getPropertyValue("camera_stream")) === CameraStreamQuality.Main ? "main" : "sub";
  }

  public get fit(): string {
    return Number(this.getPropertyValue("camera_fit")) === CameraFit.Contain ? "contain" : "cover";
  }

  public get refreshSeconds(): number {
    const value = Number(this.getPropertyValue("camera_refresh"));
    return isNaN(value) || value <= 0 ? 10 : Math.max(MIN_REFRESH_S, value);
  }

  public get isLive(): boolean {
    return this.mode === CameraMode.Live;
  }

  public get showSpinner(): boolean {
    return this.isLive ? this.state === "connecting" : !this.snapshotUrl && !this.snapshotFailed;
  }

  public get showError(): boolean {
    return !this.editMode && !!this.cameraId && (this.isLive ? this.state === "error" : this.snapshotFailed && !this.snapshotUrl);
  }

  ngOnInit() {
    this.baseOnInit();
  }

  ngAfterViewInit() {
    document.addEventListener("visibilitychange", this.visibilityHandler);

    if (typeof IntersectionObserver === "function") {
      this.observer = new IntersectionObserver(entries => {
        this.onScreen = entries.some(e => e.isIntersecting);
        this.updateStreaming();
      }, { threshold: 0.01 });
      this.observer.observe(this.host.nativeElement);
    } else {
      this.onScreen = true;
      this.updateStreaming();
    }
  }

  ngOnDestroy() {
    document.removeEventListener("visibilitychange", this.visibilityHandler);
    this.observer?.disconnect();
    this.stopTile();
    this.fullscreenPlayer?.stop();
    this.revokeSnapshot();
    super.baseOnDestroy();
  }

  /** runs the tile stream / snapshot refresh only while it can be seen */
  private updateStreaming() {
    const shouldRun = !this.editMode && !!this.cameraId && this.onScreen && document.visibilityState !== "hidden" && !this.fullscreenVisible;
    if (!shouldRun) {
      this.stopTile();
      return;
    }

    if (this.isLive) {
      this.startLive();
    } else {
      this.startSnapshots();
    }
  }

  private startLive() {
    if (this.player || !this.tileVideo) {
      return;
    }
    const video = this.tileVideo.nativeElement;
    this.player = new CameraStreamPlayer(video, () => this.socketUrl(this.quality), (state, detail) => this.zone.run(() => {
      this.state = state;
      this.stateDetail = detail;
      this.cdr.markForCheck();
    }));
    this.zone.runOutsideAngular(() => this.player.start());
  }

  private stopTile() {
    this.player?.stop();
    this.player = void 0;
    clearTimeout(this.snapshotTimer);
    this.snapshotTimer = void 0;
  }

  private startSnapshots() {
    if (this.snapshotTimer) {
      return;
    }
    const tick = async () => {
      await this.loadSnapshot();
      if (this.snapshotTimer !== void 0) {
        this.snapshotTimer = setTimeout(tick, this.refreshSeconds * 1000);
      }
    };
    this.snapshotTimer = setTimeout(tick, 0);
  }

  private async loadSnapshot() {
    try {
      const response = await fetch(`${BaseServiceHelper.getApiBaseUrl()}/camera/${encodeURIComponent(this.cameraId)}/snapshot?stream=${this.quality}`, {
        headers: { Authorization: `Bearer ${localStorage.getItem("jwt") ?? ""}` },
        cache: "no-store"
      });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }
      const blob = await response.blob();
      this.revokeSnapshot();
      this.snapshotObjectUrl = URL.createObjectURL(blob);
      this.snapshotUrl = this.snapshotObjectUrl;
      this.snapshotFailed = false;
    } catch (e) {
      this.snapshotFailed = true;
      this.stateDetail = String(e);
    }
    this.cdr.markForCheck();
  }

  private revokeSnapshot() {
    if (this.snapshotObjectUrl) {
      URL.revokeObjectURL(this.snapshotObjectUrl);
      this.snapshotObjectUrl = void 0;
    }
  }

  private socketUrl(quality: "main" | "sub") {
    return cameraWebSocketUrl(this.cameraId, quality, localStorage.getItem("jwt"));
  }

  openFullscreen(event?: Event) {
    event?.stopPropagation();
    if (this.editMode || !this.cameraId) {
      return;
    }
    this.fullscreenVisible = true;
    this.updateStreaming();
  }

  onFullscreenShown(e: any) {
    const video: HTMLVideoElement = e.component?.content()?.querySelector?.("video.camera-fullscreen-video");
    if (!video || !CameraStreamPlayer.isSupported()) {
      return;
    }
    this.fullscreenPlayer = new CameraStreamPlayer(video, () => this.socketUrl("main"), state => this.zone.run(() => {
      this.fullscreenState = state;
      this.cdr.markForCheck();
    }));
    this.zone.runOutsideAngular(() => this.fullscreenPlayer.start());
  }

  onFullscreenHiding() {
    this.fullscreenPlayer?.stop();
    this.fullscreenPlayer = void 0;
    this.fullscreenVisible = false;
    this.fullscreenState = "idle";
    this.updateStreaming();
  }
}
