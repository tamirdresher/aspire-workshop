// Minimal Express API used to demonstrate `addNodeApp` in the TypeScript
// AppHost GA sample. Aspire runs this file directly with Node.js (no build
// step required) and injects the listen port through the PORT environment
// variable via `.withHttpEndpoint({ port, env: 'PORT' })`.

const express = require('express');

const app = express();
const port = process.env.PORT || 3001;

app.get('/', (_req, res) => {
  res.json({
    message: 'Hello from the TypeScript AppHost sample API!',
    resource: 'api',
  });
});

app.get('/health', (_req, res) => {
  res.status(200).send('Healthy');
});

app.listen(port, () => {
  console.log(`api listening on port ${port}`);
});
