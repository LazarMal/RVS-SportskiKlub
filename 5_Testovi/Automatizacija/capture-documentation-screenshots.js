const fs = require("fs");
const path = require("path");
const { chromium, request } = require("playwright-core");

const out = path.resolve(process.env.RVS_RESULTS_DIR || "TestResults/documentation-screenshots");
const mvc = process.env.RVS_MVC_URL || "http://localhost:44334";
const rest = process.env.RVS_REST_URL || "http://localhost:44346";
const chrome = process.env.RVS_CHROME_PATH || "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
const adminUser = process.env.RVS_E2E_ADMIN_USER;
const adminPassword = process.env.RVS_E2E_ADMIN_PASSWORD;
const refUser = process.env.RVS_E2E_REFERENT_USER;
const refPassword = process.env.RVS_E2E_REFERENT_PASSWORD;
fs.mkdirSync(out, { recursive: true });

function assert(c, m) { if (!c) throw new Error(m); }
function iso(d) { return d.toISOString().slice(0, 10); }
function season() { const n = new Date(); const y = n.getMonth() >= 6 ? n.getFullYear() : n.getFullYear()-1; return `${y}/${String((y+1)%100).padStart(2,"0")}`; }
function payload(jmbg, prezime, overrides={}) {
  const d = new Date(); d.setMonth(d.getMonth()-1);
  return Object.assign({
    IDZahteva:0, BrojZahteva:null, JMBG:jmbg, Ime:"Dokaz", Prezime:prezime,
    DatumRodjenja:"1998-04-12", Pol:"M", Drzavljanstvo:"Srbija", Adresa:"Dokumentaciona 12",
    KontaktTelefon:"064123456", Email:`${jmbg.slice(-4)}@example.com`, IDSportskeDiscipline:1,
    NazivSportskeDiscipline:null, DatumPodnosenja:iso(new Date()), Sezona:season(), MestoKluba:"Zrenjanin",
    DatumSportskogPregleda:iso(d), RezultatTestaSposobnosti:"Položen", StatusZahteva:"U obradi",
    Napomena:"Runtime dokaz za seminarsku dokumentaciju",
    Dokumentacija:[
      {IDDokumentacije:0,NazivDokumenta:"Fotografija kandidata",Dostavljeno:true},
      {IDDokumentacije:0,NazivDokumenta:"Dokaz identiteta",Dostavljeno:true},
      {IDDokumentacije:0,NazivDokumenta:"Potvrda o sportskom pregledu",Dostavljeno:true},
      {IDDokumentacije:0,NazivDokumenta:"Evidencija o položenom testu sposobnosti",Dostavljeno:true},
      {IDDokumentacije:0,NazivDokumenta:"Saglasnost roditelja/staratelja",Dostavljeno:false},
      {IDDokumentacije:0,NazivDokumenta:"Drugi dokument",Dostavljeno:false}
    ], RoditeljStaratelj:null, IstorijaStatusa:[]
  }, overrides);
}
async function shot(page, name, full=true) { await page.screenshot({path:path.join(out,name), fullPage:full}); }
async function login(page,u,p) {
  await page.goto(`${mvc}/Nalog/Prijava`, {waitUntil:"networkidle"});
  await page.fill("#KorisnickoIme",u); await page.fill("#Sifra",p);
  await Promise.all([page.waitForLoadState("networkidle"), page.locator('input[type="submit"][value="Prijava"]').click()]);
}
async function create(api, body) {
  const r=await api.post("/api/zahtevi",{data:body}); const t=await r.text();
  assert(r.status()===200,`POST ${r.status()}: ${t}`); return JSON.parse(t);
}
async function del(api,id){ if(!id)return; const r=await api.delete(`/api/zahtevi/${id}`); assert(r.status()===200||r.status()===404,`DELETE ${id}: ${r.status()}`); }
async function approve(page,id){ await page.goto(`${mvc}/ZahtevZaUclanjenje/Detalji?id=${id}`,{waitUntil:"networkidle"}); await Promise.all([page.waitForLoadState("networkidle"),page.getByRole("button",{name:"Proveri pravilo i odobri"}).click()]); }

(async()=>{
  assert(adminUser&&adminPassword&&refUser&&refPassword,"Nedostaju CI nalozi");
  const browser=await chromium.launch({headless:true,executablePath:chrome});
  const ctx=await browser.newContext({viewport:{width:1440,height:1050}});
  const page=await ctx.newPage();
  const api=await request.newContext({baseURL:rest});
  const ids=[];
  try {
    await page.goto(`${mvc}/ZahtevZaUclanjenje/Spisak`,{waitUntil:"networkidle"});
    assert(page.url().includes("/Nalog/Prijava"),"Zaštita sesije nije preusmerila");
    await shot(page,"01-prijava.png");

    await login(page,refUser,"namerno-pogresna-lozinka");
    assert((await page.locator("body").innerText()).includes("Pogrešno korisničko ime ili lozinka"),"Nema login greške");
    await shot(page,"02-pogresna-prijava.png");

    await login(page,refUser,refPassword);
    assert(page.url().includes("/ZahtevZaUclanjenje/Spisak"),"Referent login nije uspeo");

    const valid=await create(api,payload("9100000000001","DokumentacijaValidan")); ids.push(valid.IDZahteva);
    const invalid=await create(api,payload("9100000000002","DokumentacijaOdbijen",{RezultatTestaSposobnosti:"Nije položen"})); ids.push(invalid.IDZahteva);

    await page.goto(`${mvc}/ZahtevZaUclanjenje/Spisak`,{waitUntil:"networkidle"});
    await shot(page,"03-spisak-zahteva.png");
    await page.fill('input[name="pretraga"]',"DokumentacijaValidan");
    await Promise.all([page.waitForLoadState("networkidle"),page.getByRole("button",{name:/Pretraži/}).click()]);
    await shot(page,"04-filtriranje-zahteva.png");

    await page.goto(`${mvc}/ZahtevZaUclanjenje/Dodaj`,{waitUntil:"networkidle"});
    await shot(page,"05-master-detail-forma.png");
    await page.fill("#JMBG","123");
    await page.fill("#Ime","");
    await page.fill("#Email","neispravan-email");
    await page.fill("#Sezona","2026/29");
    await page.getByRole("button",{name:"Sačuvaj zahtev i detalje"}).click();
    await page.waitForTimeout(500);
    await shot(page,"06-validacije-forme.png");

    await page.goto(`${mvc}/ZahtevZaUclanjenje/Detalji?id=${valid.IDZahteva}`,{waitUntil:"networkidle"});
    await shot(page,"07-detalji-master-detail.png");

    await approve(page,invalid.IDZahteva);
    assert(await page.locator(".alert-danger").count()===1,"Nema poruke neuspeha poslovnog pravila");
    await shot(page,"08-poslovno-pravilo-odbijanje.png");

    await approve(page,valid.IDZahteva);
    assert(await page.locator(".alert-success").count()===1,"Nema poruke uspeha poslovnog pravila");
    await shot(page,"09-poslovno-pravilo-odobrenje.png");

    await page.goto(`${mvc}/ZahtevZaUclanjenje/Izmeni?id=${valid.IDZahteva}`,{waitUntil:"networkidle"});
    await shot(page,"10-izmena-zahteva.png");

    const forbidden=await page.goto(`${mvc}/ZahtevZaUclanjenje/Obrisi?id=${valid.IDZahteva}`,{waitUntil:"networkidle"});
    assert(forbidden.status()===403,"Referent nije dobio 403");
    await shot(page,"11-referent-zabrana-brisanja.png");

    const p1=await ctx.newPage(); await p1.goto(`${mvc}/ZahtevZaUclanjenje/StampaZahteva?id=${valid.IDZahteva}`,{waitUntil:"networkidle"}); await shot(p1,"12-stampa-pojedinacnog-zahteva.png"); await p1.close();
    const p2=await ctx.newPage(); await p2.goto(`${mvc}/ZahtevZaUclanjenje/StampaSvih`,{waitUntil:"networkidle"}); await shot(p2,"13-stampa-svih.png"); await p2.close();
    const p3=await ctx.newPage(); await p3.goto(`${mvc}/ZahtevZaUclanjenje/StampaFiltriranih?pretraga=DokumentacijaValidan`,{waitUntil:"networkidle"}); await shot(p3,"14-stampa-filtriranih.png"); await p3.close();

    const restPage=await ctx.newPage(); await restPage.goto(`${rest}/api/zahtevi/${valid.IDZahteva}`,{waitUntil:"networkidle"}); await shot(restPage,"15-rest-json.png"); await restPage.close();

    await page.goto(`${mvc}/Nalog/Odjava`,{waitUntil:"networkidle"});
    await login(page,adminUser,adminPassword);
    const adminTarget=await create(api,payload("9100000000003","DokumentacijaAdminBrisanje")); ids.push(adminTarget.IDZahteva);
    await page.goto(`${mvc}/ZahtevZaUclanjenje/Obrisi?id=${adminTarget.IDZahteva}`,{waitUntil:"networkidle"});
    await shot(page,"16-administrator-brisanje.png");

    fs.writeFileSync(path.join(out,"capture-pass.txt"),"CAPTURE PASS\n","utf8");
  } finally {
    for(const id of ids){ try{await del(api,id);}catch(e){} }
    await api.dispose(); await browser.close();
  }
})().catch(e=>{console.error(e);process.exit(1);});