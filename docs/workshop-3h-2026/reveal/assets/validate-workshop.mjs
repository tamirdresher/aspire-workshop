import { existsSync, readFileSync } from "node:fs";
import { dirname, extname, resolve } from "node:path";

const root = resolve(import.meta.dirname, "../..");
const repositoryRoot = resolve(root, "../..");
const files = [
  "README.md",
  "facilitator-runbook.md",
  "student-lab-sheet.md",
  "preflight-checklist.md",
  "reveal/index.html"
];

for (const file of files) {
  const content = readFileSync(resolve(root, file), "utf8");
  if (!content.trim()) throw new Error(`${file} is empty`);
  if (new RegExp("\\." + "NET\\s+Aspire", "i").test(content)) {
    throw new Error(`${file} uses deprecated Aspire terminology`);
  }
}

const runbook = readFileSync(resolve(root, "facilitator-runbook.md"), "utf8");
const durations = [...runbook.matchAll(/\| \d\d:\d\d–\d\d:\d\d \| (\d+) \|/g)]
  .map((match) => Number(match[1]));
const total = durations.reduce((sum, duration) => sum + duration, 0);
if (total !== 180) throw new Error(`Runbook totals ${total}, expected 180 minutes`);

const deck = readFileSync(resolve(root, "reveal/index.html"), "utf8");
for (let lab = 1; lab <= 5; lab += 1) {
  const marker = `data-lab="${lab}"`;
  if (!deck.includes(marker)) throw new Error(`Deck is missing Lab ${lab}`);
  const pairing = new RegExp(
    `<section[^>]*data-concept-for="${lab}"[\\s\\S]*?<\\/section>\\s*<section[^>]*data-lab="${lab}"`,
    "m"
  );
  if (!pairing.test(deck)) {
    throw new Error(`Lab ${lab} is not immediately preceded by its concept slide`);
  }
}

const requiredLabLabels = ["Objective", "Do this", "Commands", "Success criteria", "Follow here:"];
for (const label of requiredLabLabels) {
  const count = deck.split(label).length - 1;
  if (count < 5) throw new Error(`Expected "${label}" on all five lab slides; found ${count}`);
}

const requiredConceptLabels = [
  "Concepts you must understand first",
  "Why this concept matters",
  "Core building blocks",
  "Common mistakes",
  "What success looks like",
  "Instructor explanation"
];
for (const label of requiredConceptLabels) {
  const count = deck.split(label).length - 1;
  if (count < 5) throw new Error(`Expected "${label}" on all five concept slides; found ${count}`);
}

function githubSlug(value) {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^\p{L}\p{N}\s-]/gu, "")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-");
}

function markdownAnchors(path) {
  const content = readFileSync(path, "utf8");
  return new Set(
    [...content.matchAll(/^#{1,6}\s+(.+)$/gm)]
      .map((match) => githubSlug(match[1].replace(/\s+#+$/, "")))
  );
}

function resolveRepositoryLink(sourcePath, rawLink) {
  const link = rawLink.replaceAll("&amp;", "&");
  if (link.startsWith("https://github.com/tamirdresher/aspire-workshop/blob/main/")) {
    const value = link.slice("https://github.com/tamirdresher/aspire-workshop/blob/main/".length);
    const [path, anchor] = value.split("#", 2);
    return { path: resolve(repositoryRoot, decodeURIComponent(path)), anchor };
  }

  if (/^[a-z]+:/i.test(link) || link.startsWith("#")) return null;
  const [path, anchor] = link.split("#", 2);
  return {
    path: resolve(dirname(sourcePath), decodeURIComponent(path)),
    anchor
  };
}

const linkErrors = [];
for (const relativeFile of files) {
  const sourcePath = resolve(root, relativeFile);
  const content = readFileSync(sourcePath, "utf8");
  const markdownLinks = [...content.matchAll(/\[[^\]]+\]\(([^)\s]+)(?:\s+"[^"]*")?\)/g)]
    .map((match) => match[1]);
  const htmlLinks = [...content.matchAll(/href="([^"]+)"/g)]
    .map((match) => match[1]);

  for (const rawLink of [...markdownLinks, ...htmlLinks]) {
    const target = resolveRepositoryLink(sourcePath, rawLink);
    if (!target) continue;
    if (!existsSync(target.path)) {
      linkErrors.push(`${relativeFile}: missing ${rawLink}`);
      continue;
    }
    if (target.anchor && extname(target.path).toLowerCase() === ".md") {
      const anchors = markdownAnchors(target.path);
      if (!anchors.has(target.anchor)) {
        linkErrors.push(`${relativeFile}: missing anchor #${target.anchor} in ${target.path}`);
      }
    }
  }
}

if (linkErrors.length) {
  throw new Error(`Invalid repository links:\n${linkErrors.join("\n")}`);
}

console.log(`Workshop package valid: ${files.length} deliverables, 5 labs, ${total} minutes, repository links checked.`);
