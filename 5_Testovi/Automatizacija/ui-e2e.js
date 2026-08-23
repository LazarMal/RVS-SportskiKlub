const fs = require("fs");
const path = require("path");
const { chromium, request } = require("playwright-core");

const repoRoot = path.resolve(process.env.RVS_REPO_ROOT || path.join(__dirname, "..", ".."));
const resultsDirectory = path.resolve(process.env.RVS_RESULTS_DIR || path.join(repoRoot, "TestResults"));
const mvcBase = process.env.RVS_MVC_URL || "http://localhost:44334";
const restBase = process.env.RVS_REST_URL || "http://localhost:44346";
const chromePath = process.env.RVS_CHROME_PATH || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const adminUser = process.env.RVS_E2E_ADMIN_USER;
const adminPassword = process.env.RVS_E2E_ADMIN_PASSWORD;
const referentUser = process.env.RVS_E2E_REFERENT_USER;
const referentPassword = process.env.RVS_E2E_REFERENT_PASSWORD;
const report = { startedAt: new Date().toISOString(), passed: [], counts: {} };

fs.mkdirSync(resultsDirectory, { recursive: true });
assert(adminUser && adminPassword && referentUser && referentPassword, "Nedostaju privremeni RVS_E2E nalozi iz database testa.");

async function capture(page, fileName) {
  await page.screenshot({ path: path.join(resultsDirectory, fileName), fullPage: true });
}

function pass(name, count = 1) {
  report.passed.push(name);
  report.counts[name] = (report.counts[name] || 0) + count;
  process.stdout.write(`PASS: ${name}${count > 1 ? ` x${count}` : ""}\n`);
}

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function isoDate(date) {
  return date.toISOString().slice(0, 10);
}

function addMonths(months, extraDays = 0) {
  const now = new Date();
  const originalDay = now.getDate();
  const result = new Date(now.getFullYear(), now.getMonth() + months, 1);
  const lastDay = new Date(result.getFullYear(), result.getMonth() + 1, 0).getDate();
  result.setDate(Math.min(originalDay, lastDay));
  result.setDate(result.getDate() + extraDays);
  return result;
}

function currentSeason() {
  const now = new Date();
  const start = now.getMonth() >= 6 ? now.getFullYear() : now.getFullYear() - 1;
  return `${start}/${String((start + 1) % 100).padStart(2, "0")}`;
}

function payload(number, overrides = {}) {
  const value = {
    IDZahteva: 0,
    BrojZahteva: null,
    JMBG: String(number).padStart(13, "0"),
    Ime: "E2E",
    Prezime: `Kandidat${number}`,
    DatumRodjenja: "1994-04-12",
    Pol: "M",
    Drzavljanstvo: "Srbija",
    Adresa: "E2E adresa 1",
    KontaktTelefon: "064123456",
    Email: `e2e${number}@example.com`,
    IDSportskeDiscipline: 1,
    NazivSportskeDiscipline: null,
    DatumPodnosenja: isoDate(new Date()),
    Sezona: currentSeason(),
    MestoKluba: "Zrenjanin",
    DatumSportskogPregleda: isoDate(addMonths(-1)),
    RezultatTestaSposobnosti: "Položen",
    StatusZahteva: "U obradi",
    Napomena: "Automatizovani E2E test",
    Dokumentacija: [
      { IDDokumentacije: 0, NazivDokumenta: "Fotografija kandidata", Dostavljeno: true },
      { IDDokumentacije: 0, NazivDokumenta: "Dokaz identiteta", Dostavljeno: true },
      { IDDokumentacije: 0, NazivDokumenta: "Potvrda o sportskom pregledu", Dostavljeno: true },
      { IDDokumentacije: 0, NazivDokumenta: "Evidencija o položenom testu sposobnosti", Dostavljeno: true },
      { IDDokumentacije: 0, NazivDokumenta: "Saglasnost roditelja/staratelja", Dostavljeno: false },
      { IDDokumentacije: 0, NazivDokumenta: "Drugi dokument", Dostavljeno: false }
    ],
    RoditeljStaratelj: null,
    IstorijaStatusa: []
  };
  return Object.assign(value, overrides);
}

async function apiCreate(api, body) {
  const response = await api.post("/api/zahtevi", { data: body });
  const text = await response.text();
  assert(response.status() === 200, `REST POST nije prošao (${response.status()}): ${text}`);
  return JSON.parse(text);
}

async function apiDelete(api, id) {
  const response = await api.delete(`/api/zahtevi/${id}`);
  assert(response.status() === 200, `REST DELETE ${id} nije prošao: ${response.status()} ${await response.text()}`);
}

async function waitForParameter(api, expectedValue) {
  let lastError;
  for (let attempt = 1; attempt <= 30; attempt++) {
    try {
      const response = await api.get("/api/parametri/poslovna-pravila");
      if (response.status() === 200) {
        const value = await response.json();
        if (value.MaksimalnaStarostSportskogPregledaMeseci === expectedValue) return value;
      }
    } catch (error) {
      lastError = error;
    }
    await new Promise(resolve => setTimeout(resolve, 500));
  }
  throw new Error(`REST parametar nije postao X=${expectedValue}. ${lastError || ""}`);
}

async function login(page, username, password) {
  await page.goto(`${mvcBase}/Nalog/Prijava`, { waitUntil: "networkidle" });
  await page.fill("#KorisnickoIme", username);
  await page.fill("#Sifra", password);
  await Promise.all([
    page.waitForLoadState("networkidle"),
    page.locator('input[type="submit"][value="Prijava"]').click()
  ]);
}

async function logout(page) {
  await page.goto(`${mvcBase}/Nalog/Odjava`, { waitUntil: "networkidle" });
}

async function readStatus(page) {
  const status = page.locator("dt", { hasText: "Status" }).locator("xpath=following-sibling::dd[1]//strong");
  return (await status.first().innerText()).trim();
}

async function approveAndCheck(page, id, expectedSuccess) {
  await page.goto(`${mvcBase}/ZahtevZaUclanjenje/Detalji?id=${id}`, { waitUntil: "networkidle" });
  await Promise.all([
    page.waitForLoadState("networkidle"),
    page.getByRole("button", { name: "Proveri pravilo i odobri" }).click()
  ]);
  const status = await readStatus(page);
  if (expectedSuccess) {
    assert(status === "Odobren", `Zahtev ${id} nije odobren; status je '${status}'.`);
    assert(await page.locator(".alert-success").count() === 1, `Nedostaje uspešna poruka za zahtev ${id}.`);
  } else {
    assert(status !== "Odobren", `Nevalidan zahtev ${id} je pogrešno odobren.`);
    assert(await page.locator(".alert-danger").count() === 1, `Nedostaje poruka odbijanja za zahtev ${id}.`);
  }
}

async function createThroughMvc(page, iteration) {
  const jmbg = String(8700000000000 + iteration);
  const surname = `MVC${iteration}`;
  await page.goto(`${mvcBase}/ZahtevZaUclanjenje/Dodaj`, { waitUntil: "networkidle" });
  if (iteration === 1) {
    await capture(page, "forma-unos.png");
    await page.getByRole("button", { name: "Sačuvaj zahtev i detalje" }).click();
    await page.waitForTimeout(250);
    await capture(page, "validacije-unosa.png");
  }
  const options = await page.locator("#IDSportskeDiscipline option").count();
  assert(options === 7, `Dropdown mora imati placeholder i šest disciplina; ima ${options}.`);

  await page.fill("#JMBG", jmbg);
  await page.fill("#Ime", "Browser");
  await page.fill("#Prezime", surname);
  await page.fill("#DatumRodjenja", "1993-03-15");
  await page.selectOption("#Pol", "M");
  await page.fill("#Drzavljanstvo", "Srbija");
  await page.fill("#Adresa", "Browser adresa 1");
  await page.fill("#KontaktTelefon", "064555666");
  await page.fill("#Email", `browser${iteration}@example.com`);
  await page.selectOption("#IDSportskeDiscipline", "1");
  await page.fill("#Sezona", currentSeason());
  await page.fill("#MestoKluba", "Zrenjanin");
  await page.fill("#DatumSportskogPregleda", isoDate(addMonths(-1)));
  await page.selectOption("#RezultatTestaSposobnosti", { label: "Položen" });
  await page.fill("#Napomena", `MVC E2E krug ${iteration}`);
  await page.locator('label:has-text("Potvrda o sportskom pregledu") input[type="checkbox"]').check();
  await page.locator('label:has-text("Evidencija o položenom testu sposobnosti") input[type="checkbox"]').check();
  if (iteration === 1) await capture(page, "forma-unos-popunjena.png");

  await Promise.all([
    page.waitForLoadState("networkidle"),
    page.getByRole("button", { name: "Sačuvaj zahtev i detalje" }).click()
  ]);
  assert(page.url().includes("/Detalji"), `MVC unos nije preusmerio na detalje: ${page.url()}`);
  const match = page.url().match(/(?:Detalji\/|[?&]id=)(\d+)/i);
  assert(match, `ID zahteva nije pronađen u URL-u: ${page.url()}`);
  const id = Number(match[1]);
  const bodyText = await page.locator("body").innerText();
  assert(bodyText.includes(`Browser ${surname}`), "Detalji ne prikazuju kreiranog kandidata.");
  assert(bodyText.includes("U obradi"), "Početni status nije U obradi.");
  assert(bodyText.includes("Zahtev je evidentiran"), "Početna istorija nije prikazana.");
  return { id, jmbg, surname };
}

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: chromePath });
  const context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1440, height: 1000 } });
  const page = await context.newPage();
  const api = await request.newContext({ baseURL: restBase, ignoreHTTPSErrors: true });

  try {
    for (let i = 1; i <= 10; i++) {
      const protectedResponse = await page.goto(`${mvcBase}/ZahtevZaUclanjenje/Spisak`, { waitUntil: "networkidle" });
      assert(protectedResponse.ok(), `Zaštićena ruta nije odgovorila u krugu ${i}.`);
      assert(page.url().includes("/Nalog/Prijava"), `Ruta bez sesije nije preusmerila na prijavu u krugu ${i}.`);
      assert(await page.locator(".app-sidebar").count() === 0, `Bočni meni je vidljiv pre prijave u krugu ${i}.`);
      assert(await page.locator(".guest-brand-rail").count() === 1, `Sportski identitet nije prikazan pre prijave u krugu ${i}.`);
      assert(await page.locator(".guest-brand-rail a").count() === 0, `Gostujući sportski identitet ne sme sadržati linkove u krugu ${i}.`);
      assert(await page.locator(".guest-brand-rail .app-nav").count() === 0, `Gostujući sportski identitet ne sme sadržati navigaciju u krugu ${i}.`);
      assert(await page.locator(".app-main-guest").count() === 1, `Gostujući raspored nije aktivan u krugu ${i}.`);
      if (i === 1) await capture(page, "login.png");
    }
    pass("zaštita rute bez sesije", 10);
    pass("bočni meni skriven pre prijave", 10);
    pass("sportski identitet bez navigacije pre prijave", 10);

    for (let i = 1; i <= 10; i++) {
      await login(page, adminUser, `namerno-pogresno-${i}`);
      assert((await page.locator("body").innerText()).includes("Pogrešno korisničko ime ili lozinka"), `Pogrešan login nije odbijen u krugu ${i}.`);
      if (i === 1) {
        await page.fill("#Sifra", "");
        await capture(page, "login-neuspesan.png");
      }
    }
    pass("pogrešan login", 10);

    for (let i = 1; i <= 10; i++) {
      await login(page, referentUser, referentPassword);
      assert(page.url().includes("/ZahtevZaUclanjenje/Spisak"), `Referent login nije uspeo u krugu ${i}.`);
      assert(await page.locator(".app-sidebar").count() === 1, `Bočni meni nije prikazan posle prijave u krugu ${i}.`);
      assert(await page.locator(".app-main-guest").count() === 0, `Gostujući raspored je ostao aktivan posle prijave u krugu ${i}.`);
      assert(await page.locator('.app-sidebar a[href*="/ZahtevZaUclanjenje/Dodaj"]').count() === 1, `Link Novi zahtev nedostaje posle prijave u krugu ${i}.`);
      assert(await page.locator('.app-sidebar a[href*="/ZahtevZaUclanjenje/Spisak"]').count() === 2, `Linkovi ka spisku zahteva nisu prikazani posle prijave u krugu ${i}.`);
      assert(await page.locator('.app-sidebar a[href*="/api/zahtevi"]').count() === 1, `Link REST API nedostaje posle prijave u krugu ${i}.`);
      await logout(page);
    }
    pass("uspešan login preko Stored Procedure", 10);
    pass("bočni meni prikazan posle prijave", 10);
    await login(page, referentUser, referentPassword);
    await capture(page, "spisak-zahteva.png");

    const restParameterPage = await context.newPage();
    await restParameterPage.goto(`${restBase}/api/parametri/poslovna-pravila`, { waitUntil: "networkidle" });
    await capture(restParameterPage, "rest-parametri-json.png");
    await restParameterPage.close();

    for (let i = 1; i <= 10; i++) {
      const created = await createThroughMvc(page, i);
      const referentDelete = await page.goto(`${mvcBase}/ZahtevZaUclanjenje/Obrisi?id=${created.id}`, { waitUntil: "networkidle" });
      assert(referentDelete.status() === 403, `Referent je dobio brisanje u krugu ${i}.`);

      await page.goto(`${mvcBase}/ZahtevZaUclanjenje/Detalji?id=${created.id}`, { waitUntil: "networkidle" });
      await approveAndCheck(page, created.id, true);
      if (i === 1) await capture(page, "detalji-odobren.png");

      const printPage = await context.newPage();
      await printPage.goto(`${mvcBase}/ZahtevZaUclanjenje/StampaZahteva?id=${created.id}`, { waitUntil: "networkidle" });
      const printText = await printPage.locator("body").innerText();
      if (i === 1) {
        await printPage.screenshot({ path: path.join(resultsDirectory, "stampa-zahteva.png"), fullPage: true });
        await printPage.pdf({ path: path.join(resultsDirectory, "stampa-zahteva.pdf"), format: "A4", printBackground: true });
      }
      const normalizedPrintText = printText.toUpperCase();
      assert(normalizedPrintText.includes("SPORTSKI KLUB") && normalizedPrintText.includes("MLADOST"), "Pojedinačna štampa nema propisano zaglavlje.");
      assert(printText.includes(created.jmbg), "Pojedinačna štampa nema JMBG kandidata.");
      assert(printText.includes("Potvrda o sportskom pregledu"), "Pojedinačna štampa nema dokumentaciju.");
      await printPage.close();

      await page.goto(`${mvcBase}/ZahtevZaUclanjenje/Izmeni?id=${created.id}`, { waitUntil: "networkidle" });
      if (i === 1) await capture(page, "izmena-zahteva.png");
      await page.fill("#KontaktTelefon", `06466${String(i).padStart(4, "0")}`);
      await page.fill("#Napomena", `Izmenjeno kroz browser ${i}`);
      await Promise.all([
        page.waitForLoadState("networkidle"),
        page.getByRole("button", { name: "Sačuvaj izmene" }).click()
      ]);
      assert((await readStatus(page)) === "Na proveri", "Izmena odobrenog zahteva nije vratila status Na proveri.");

      await page.goto(`${mvcBase}/ZahtevZaUclanjenje/Spisak`, { waitUntil: "networkidle" });
      await page.fill('input[name="pretraga"]', created.surname);
      await Promise.all([
        page.waitForLoadState("networkidle"),
        page.getByRole("button", { name: "Pretraži" }).click()
      ]);
      const rows = await page.locator("table.data-table tbody tr").count();
      assert(rows === 1, `Filter nije vratio tačno jedan red u krugu ${i}; vratio je ${rows}.`);
      if (i === 1) await capture(page, "filtriranje-zahteva.png");

      const filteredPrint = await context.newPage();
      await filteredPrint.goto(`${mvcBase}/ZahtevZaUclanjenje/StampaFiltriranih?pretraga=${encodeURIComponent(created.surname)}`, { waitUntil: "networkidle" });
      const filteredText = await filteredPrint.locator("body").innerText();
      assert(filteredText.includes(created.surname), "Filtrirana štampa nema očekivani zahtev.");
      if (i === 1) {
        await filteredPrint.screenshot({ path: path.join(resultsDirectory, "stampa-filtriranih.png"), fullPage: true });
        await filteredPrint.pdf({ path: path.join(resultsDirectory, "stampa-filtriranih.pdf"), preferCSSPageSize: true, printBackground: true });
      }
      await filteredPrint.close();

      await apiDelete(api, created.id);
    }
    pass("MVC create/detail/business/edit/filter/tri štampe", 10);
    pass("referent ne može da briše", 10);

    for (let i = 1; i <= 10; i++) {
      const allPrint = await context.newPage();
      await allPrint.goto(`${mvcBase}/ZahtevZaUclanjenje/StampaSvih`, { waitUntil: "networkidle" });
      assert((await allPrint.locator("body").innerText()).includes("Spisak svih zahteva za učlanjenje"), `Štampa svih nema očekivani naslov u krugu ${i}.`);
      if (i === 1) {
        await allPrint.screenshot({ path: path.join(resultsDirectory, "stampa-svih.png"), fullPage: true });
        await allPrint.pdf({ path: path.join(resultsDirectory, "stampa-svih.pdf"), preferCSSPageSize: true, printBackground: true });
      }
      await allPrint.close();
    }
    pass("štampa svih zahteva", 10);

    const businessCases = [
      { name: "validan", date: isoDate(addMonths(-1)), result: "Položen", expected: true },
      { name: "stariji od X", date: isoDate(addMonths(-6, -1)), result: "Položen", expected: false },
      { name: "nije položen", date: isoDate(addMonths(-1)), result: "Nije položen", expected: false },
      { name: "nije realizovan", date: isoDate(addMonths(-1)), result: "Nije realizovan", expected: false },
      { name: "tačno X", date: isoDate(addMonths(-6)), result: "Položen", expected: true }
    ];

    for (let round = 1; round <= 10; round++) {
      for (let index = 0; index < businessCases.length; index++) {
        const scenario = businessCases[index];
        const number = 8800000000000 + round * 10 + index;
        const created = await apiCreate(api, payload(number, {
          DatumSportskogPregleda: scenario.date,
          RezultatTestaSposobnosti: scenario.result,
          Prezime: `Pravilo${round}_${index}`
        }));
        await approveAndCheck(page, created.IDZahteva, scenario.expected);
        if (round === 1 && index === 1) await capture(page, "poslovno-pravilo-odbijeno.png");
        await apiDelete(api, created.IDZahteva);
      }
    }
    pass("pet poslovnih scenarija kroz MVC+REST+EF", 50);

    const parameterFile = path.join(repoRoot, "3_SlojServisa", "RESTApi", "RESTServis", "RESTServis", "App_Data", "poslovna_pravila.json");
    const originalParameters = fs.readFileSync(parameterFile, "utf8");
    try {
      const changed = JSON.parse(originalParameters);
      changed.MaksimalnaStarostSportskogPregledaMeseci = 5;
      fs.writeFileSync(parameterFile, JSON.stringify(changed, null, 2), "utf8");
      await waitForParameter(api, 5);
      for (let i = 1; i <= 10; i++) {
        const created = await apiCreate(api, payload(8900000000000 + i, {
          DatumSportskogPregleda: isoDate(addMonths(-6)),
          Prezime: `Parametar${i}`
        }));
        await approveAndCheck(page, created.IDZahteva, false);
        await apiDelete(api, created.IDZahteva);
      }
    } finally {
      fs.writeFileSync(parameterFile, originalParameters, "utf8");
      await waitForParameter(api, 6);
    }
    pass("promena REST/JSON parametra X", 10);

    await logout(page);
    await login(page, adminUser, adminPassword);
    for (let i = 1; i <= 10; i++) {
      const created = await apiCreate(api, payload(8950000000000 + i, { Prezime: `AdminBrisanje${i}` }));
      await page.goto(`${mvcBase}/ZahtevZaUclanjenje/Obrisi?id=${created.IDZahteva}`, { waitUntil: "networkidle" });
      if (i === 1) await capture(page, "brisanje-administrator.png");
      await Promise.all([
        page.waitForLoadState("networkidle"),
        page.getByRole("button", { name: "Potvrdi brisanje" }).click()
      ]);
      const missing = await api.get(`/api/zahtevi/${created.IDZahteva}`);
      assert(missing.status() === 404, `Administratorsko brisanje nije uklonilo zahtev ${created.IDZahteva}.`);
    }
    pass("administratorsko MVC brisanje", 10);

    for (let i = 1; i <= 10; i++) {
      await logout(page);
      await page.goto(`${mvcBase}/ZahtevZaUclanjenje/Spisak`, { waitUntil: "networkidle" });
      assert(page.url().includes("/Nalog/Prijava"), `Logout nije uklonio sesiju u krugu ${i}.`);
      if (i < 10) {
        await login(page, adminUser, adminPassword);
      }
    }
    pass("logout i ponovno zaključavanje sesije", 10);

    report.finishedAt = new Date().toISOString();
    fs.writeFileSync(path.join(resultsDirectory, "ui-e2e-report.json"), JSON.stringify(report, null, 2), "utf8");
    process.stdout.write("UI E2E PASS: svi browser tokovi su prošli.\n");
  } catch (error) {
    report.finishedAt = new Date().toISOString();
    report.error = error.stack || String(error);
    fs.writeFileSync(path.join(resultsDirectory, "ui-e2e-report.json"), JSON.stringify(report, null, 2), "utf8");
    await page.screenshot({ path: path.join(resultsDirectory, "ui-failure.png"), fullPage: true }).catch(() => {});
    throw error;
  } finally {
    await api.dispose();
    await context.close();
    await browser.close();
  }
})().catch(error => {
  console.error(error);
  process.exit(1);
});
