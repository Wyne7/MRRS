const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT
  ? `https://127.0.0.1:${env.ASPNETCORE_HTTPS_PORT}`
  : env.ASPNETCORE_URLS
    ? env.ASPNETCORE_URLS.split(';')[0]
    : 'http://127.0.0.1:43123';

module.exports = [
  {
    context: ['/api', '/host'],
    target,
    secure: false,
    changeOrigin: true,
    headers: {
      Connection: 'Keep-Alive'
    }
  }
];
