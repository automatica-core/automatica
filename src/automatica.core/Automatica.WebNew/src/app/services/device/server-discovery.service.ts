import { Injectable, NgZone } from "@angular/core";
import { Capacitor, PluginListenerHandle, registerPlugin } from "@capacitor/core";

/** A server found in the local network, announced by Automatica.Discovery/MdnsAdvertiser.cs. */
export interface DiscoveredServer {
    name: string;
    host: string;
    port: number;
    scheme: string;
    url: string;
    id: string;
    version: string;
}

interface ServerDiscoveryPlugin {
    start(): Promise<void>;
    stop(): Promise<void>;
    addListener(eventName: "serverFound", listener: (server: DiscoveredServer) => void): Promise<PluginListenerHandle>;
    addListener(eventName: "serverLost", listener: (server: { name: string }) => void): Promise<PluginListenerHandle>;
}

// native part: android/app/src/main/java/at/automatica/core/ServerDiscoveryPlugin.java
const ServerDiscovery = registerPlugin<ServerDiscoveryPlugin>("ServerDiscovery");

@Injectable({ providedIn: "root" })
export class ServerDiscoveryService {

    constructor(private zone: NgZone) { }

    /** Only the native app can browse mDNS, and so far only Android has the plugin. */
    get isAvailable(): boolean {
        return Capacitor.isNativePlatform() && Capacitor.isPluginAvailable("ServerDiscovery");
    }

    /**
     * Starts browsing; onChange gets the current list whenever a server shows up or goes away.
     * Returns the function that stops the discovery again.
     */
    async start(onChange: (servers: DiscoveredServer[]) => void): Promise<() => Promise<void>> {
        if (!this.isAvailable) {
            return async () => { };
        }

        // keyed by server id: a server announced on two interfaces shows up once
        const servers = new Map<string, DiscoveredServer>();
        const emit = () => this.zone.run(() => onChange([...servers.values()]));

        const handles = await Promise.all([
            ServerDiscovery.addListener("serverFound", server => {
                servers.set(server.id || server.url, server);
                emit();
            }),
            ServerDiscovery.addListener("serverLost", lost => {
                for (const [key, server] of servers) {
                    if (server.name === lost.name) {
                        servers.delete(key);
                    }
                }
                emit();
            })
        ]);

        try {
            await ServerDiscovery.start();
        } catch (e) {
            console.warn("server discovery could not be started", e);
        }

        return async () => {
            await Promise.all(handles.map(h => h.remove()));
            await ServerDiscovery.stop().catch(() => { });
        };
    }
}
