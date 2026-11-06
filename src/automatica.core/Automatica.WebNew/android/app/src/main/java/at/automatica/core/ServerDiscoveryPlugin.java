package at.automatica.core;

import android.content.Context;
import android.net.nsd.NsdManager;
import android.net.nsd.NsdServiceInfo;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;

import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;

import java.net.Inet4Address;
import java.net.InetAddress;
import java.nio.charset.StandardCharsets;
import java.util.ArrayDeque;
import java.util.Map;

/**
 * Finds Automatica.Core servers in the local network. The server announces itself via mDNS
 * as "_automatica._tcp" (Automatica.Discovery/MdnsAdvertiser.cs).
 *
 * Events: "serverFound" {name, host, port, scheme, url, id, version}, "serverLost" {name}.
 */
@CapacitorPlugin(name = "ServerDiscovery")
public class ServerDiscoveryPlugin extends Plugin {

    private static final String TAG = "ServerDiscovery";
    private static final String SERVICE_TYPE = "_automatica._tcp.";

    private NsdManager nsdManager;
    private NsdManager.DiscoveryListener discoveryListener;

    // resolveService only handles one request at a time on older Android versions
    private final ArrayDeque<NsdServiceInfo> resolveQueue = new ArrayDeque<>();
    private boolean resolving = false;
    private final Handler mainHandler = new Handler(Looper.getMainLooper());

    @Override
    public void load() {
        nsdManager = (NsdManager) getContext().getSystemService(Context.NSD_SERVICE);
    }

    @PluginMethod
    public void start(PluginCall call) {
        stopDiscovery();

        discoveryListener = new NsdManager.DiscoveryListener() {
            @Override
            public void onStartDiscoveryFailed(String serviceType, int errorCode) {
                Log.w(TAG, "discovery failed to start: " + errorCode);
                discoveryListener = null;
            }

            @Override
            public void onStopDiscoveryFailed(String serviceType, int errorCode) {
                Log.w(TAG, "discovery failed to stop: " + errorCode);
            }

            @Override
            public void onDiscoveryStarted(String serviceType) {
                Log.d(TAG, "discovery started");
            }

            @Override
            public void onDiscoveryStopped(String serviceType) {
                Log.d(TAG, "discovery stopped");
            }

            @Override
            public void onServiceFound(NsdServiceInfo serviceInfo) {
                mainHandler.post(() -> enqueueResolve(serviceInfo));
            }

            @Override
            public void onServiceLost(NsdServiceInfo serviceInfo) {
                JSObject lost = new JSObject();
                lost.put("name", serviceInfo.getServiceName());
                notifyListeners("serverLost", lost);
            }
        };

        try {
            nsdManager.discoverServices(SERVICE_TYPE, NsdManager.PROTOCOL_DNS_SD, discoveryListener);
            call.resolve();
        } catch (Exception e) {
            discoveryListener = null;
            call.reject("Could not start the server discovery", e);
        }
    }

    @PluginMethod
    public void stop(PluginCall call) {
        stopDiscovery();
        call.resolve();
    }

    @Override
    protected void handleOnDestroy() {
        stopDiscovery();
    }

    private void stopDiscovery() {
        if (discoveryListener != null) {
            try {
                nsdManager.stopServiceDiscovery(discoveryListener);
            } catch (IllegalArgumentException ignored) {
                // was not running (start failed)
            }
            discoveryListener = null;
        }
        mainHandler.post(() -> {
            resolveQueue.clear();
            resolving = false;
        });
    }

    private void enqueueResolve(NsdServiceInfo info) {
        resolveQueue.add(info);
        resolveNext();
    }

    @SuppressWarnings("deprecation") // resolveService is the only resolve API below Android 14
    private void resolveNext() {
        if (resolving || resolveQueue.isEmpty() || discoveryListener == null) {
            return;
        }
        resolving = true;
        NsdServiceInfo next = resolveQueue.poll();

        nsdManager.resolveService(next, new NsdManager.ResolveListener() {
            @Override
            public void onResolveFailed(NsdServiceInfo serviceInfo, int errorCode) {
                Log.w(TAG, "could not resolve " + serviceInfo.getServiceName() + ": " + errorCode);
                mainHandler.post(() -> {
                    resolving = false;
                    resolveNext();
                });
            }

            @Override
            public void onServiceResolved(NsdServiceInfo serviceInfo) {
                publish(serviceInfo);
                mainHandler.post(() -> {
                    resolving = false;
                    resolveNext();
                });
            }
        });
    }

    @SuppressWarnings("deprecation") // getHost is the only host API below Android 14
    private void publish(NsdServiceInfo info) {
        InetAddress host = info.getHost();
        if (host == null) {
            return;
        }

        String address = host.getHostAddress();
        if (!(host instanceof Inet4Address)) {
            // IPv6 literal in a URL needs brackets, scope ids are not allowed there
            int scope = address.indexOf('%');
            address = "[" + (scope >= 0 ? address.substring(0, scope) : address) + "]";
        }

        Map<String, byte[]> txt = info.getAttributes();
        String scheme = txtValue(txt, "scheme", "http");
        String name = txtValue(txt, "name", info.getServiceName());
        int port = info.getPort();

        // the app runs on https://localhost, a plain http server is blocked as mixed content
        String httpsPort = txtValue(txt, "https_port", "");
        if (!httpsPort.isEmpty()) {
            try {
                port = Integer.parseInt(httpsPort);
                scheme = "https";
            } catch (NumberFormatException ignored) {
                // keep the http port
            }
        }

        JSObject found = new JSObject();
        found.put("name", name);
        found.put("host", address);
        found.put("port", port);
        found.put("scheme", scheme);
        found.put("url", scheme + "://" + address + ":" + port);
        found.put("id", txtValue(txt, "id", ""));
        found.put("version", txtValue(txt, "version", ""));
        notifyListeners("serverFound", found);
    }

    private static String txtValue(Map<String, byte[]> txt, String key, String fallback) {
        if (txt == null) {
            return fallback;
        }
        byte[] value = txt.get(key);
        if (value == null || value.length == 0) {
            return fallback;
        }
        return new String(value, StandardCharsets.UTF_8);
    }
}
