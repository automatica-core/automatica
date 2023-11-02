import { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'at.automatica.core',
  appName: 'automatica.core',
  webDir: 'dist',
  server: {
    androidScheme: 'https'
  }
};

export default config;
