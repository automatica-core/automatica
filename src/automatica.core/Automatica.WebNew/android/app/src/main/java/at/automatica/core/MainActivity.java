package at.automatica.core;

import android.net.http.SslCertificate;
import android.net.http.SslError;
import android.os.Bundle;
import android.webkit.SslErrorHandler;
import android.webkit.WebView;

import com.getcapacitor.BridgeActivity;
import com.getcapacitor.BridgeWebViewClient;
import com.getcapacitor.Plugin;

public class MainActivity extends BridgeActivity {
  @Override
  public void onCreate(Bundle savedInstanceState) {
    super.onCreate(savedInstanceState);

    this.bridge.getWebView().setWebViewClient(new BridgeWebViewClient(this.bridge) {
      @Override
      public void onReceivedSslError(WebView view, final SslErrorHandler handler, SslError error) {
        SslCertificate serverCertificate = error.getCertificate();
        if(serverCertificate.getIssuedBy().getCName().equals("localhost") && serverCertificate.getIssuedTo().getCName().equals("localhost"))
          handler.proceed();
        else
          handler.cancel();
      }
    });

  }
}
