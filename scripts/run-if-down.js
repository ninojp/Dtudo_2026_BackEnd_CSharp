import http from "node:http";
import https from "node:https";
import net from "node:net";
import { spawn } from "node:child_process";

const [healthUrl, separator, ...commandParts] = process.argv.slice(2);

if (!healthUrl || separator !== "--" || commandParts.length === 0) {
  console.error("Uso: node scripts/run-if-down.js <health-url> -- <command>");
  process.exit(1);
}

const pollIntervalMs = Number(process.env.DTUDO_HEALTH_POLL_INTERVAL_MS ?? 5000);
const timeoutMs = Number(process.env.DTUDO_HEALTH_TIMEOUT_MS ?? 2500);

async function requestHealth(url) {
  return new Promise((resolve) => {
    const parsedUrl = new URL(url);
    const client = parsedUrl.protocol === "https:" ? https : http;
    const request = client.request(
      parsedUrl,
      {
        method: "GET",
        timeout: timeoutMs,
        rejectUnauthorized: false,
      },
      (response) => {
        response.resume();
        response.on("end", () => resolve(response.statusCode >= 200 && response.statusCode < 300));
      },
    );

    request.on("timeout", () => {
      request.destroy();
      resolve(false);
    });
    request.on("error", () => resolve(false));
    request.end();
  });
}

async function hasOpenPort(url) {
  return new Promise((resolve) => {
    const parsedUrl = new URL(url);
    const port = Number(parsedUrl.port || (parsedUrl.protocol === "https:" ? 443 : 80));
    const socket = net.connect({ host: parsedUrl.hostname, port, timeout: timeoutMs }, () => {
      socket.destroy();
      resolve(true);
    });

    socket.on("timeout", () => {
      socket.destroy();
      resolve(false);
    });
    socket.on("error", () => resolve(false));
  });
}

function waitForever(message) {
  console.log(message);
  setInterval(async () => {
    if (!(await requestHealth(healthUrl))) {
      console.warn(`[dtudo] ${healthUrl} parou de responder.`);
    }
  }, pollIntervalMs);
}

if (await requestHealth(healthUrl)) {
  waitForever(`[dtudo] ${healthUrl} ja esta rodando. Mantendo este processo ativo para o concurrently.`);
} else if (await hasOpenPort(healthUrl)) {
  console.error(`[dtudo] A porta de ${healthUrl} esta ocupada, mas o health check nao respondeu OK.`);
  console.error("[dtudo] Feche o processo nessa porta ou ajuste a URL configurada antes de iniciar a stack.");
  process.exit(1);
} else {
  const command = commandParts.join(" ");
  console.log(`[dtudo] ${healthUrl} fora do ar. Iniciando: ${command}`);

  const child = spawn(command, {
    stdio: "inherit",
    shell: true,
    env: process.env,
  });

  child.on("exit", (code, signal) => {
    if (signal) {
      process.kill(process.pid, signal);
      return;
    }

    process.exit(code ?? 0);
  });
}
