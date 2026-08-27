# Demo 3: Autonomiczny agent AI w CI/CD - prompt injection przez tresc issue

> ⚠️ REPOZYTORIUM EDUKACYJNE. Wszystkie sekrety w `appsettings.json` sa falszywe (FAKE_*).
> `examples/poisoned-issue.md` zawiera DEMONSTRACYJNY, zlosliwy payload wzorowany na realnych
> incydentach 2025 (Amazon Q Developer Extension, GitLab Duo - zob. `../../materials/incidents/cicd-prompt-injection.md`).
> Ten payload jest niewidoczny w renderowanym Markdown na GitHubie (komentarz HTML), ale w pelni
> widoczny dla agenta AI odczytujacego surowa tresc issue - to jest sedno ataku.

## Scenariusz ataku

1. Atakujacy (albo, w tym demo, sam prezenter) tworzy nowe issue w repozytorium, uzywajac tresci
   z `examples/poisoned-issue.md` - zgloszenie wyglada jak normalny bug report, ale zawiera ukryty
   komentarz HTML z instrukcja dla agenta AI.
2. Workflow `.github/workflows/auto-fix.yml` uruchamia `AutoFixAgent` na nowym issue: agent
   odczytuje pelna, surowa tresc issue (w tym ukryty komentarz), prosi model Claude (przez Amazon
   Bedrock) o poprawiony plik, i otwiera pull request z etykieta `ai-generated`.
3. Model, "pomocnie" wykonujac ukryta instrukcje, dodaje do poprawki dodatkowy endpoint
   `/debug/env`, ktory zrzuca zmienne srodowiskowe procesu i wysyla je przez `HttpClient` do
   zewnetrznego adresu - backdoor/eksfiltracja secrets w tej samej "poprawce buga".
4. `.github/workflows/auto-merge-vulnerable.yml` - naiwna konfiguracja pipeline'u, ktora
   automatycznie merguje KAZDY PR z etykieta `ai-generated`, bez wymogu human review - PR z
   backdoorem zostaje zmergowany do `main`.

## Live demo - krok po kroku (wymaga wlasnych AWS credentials z dostepem do Bedrock)

> Zawartosc tego katalogu (`.github/`, `src/`, `examples/`, `README.md`) - a NIE sam katalog
> `03-cicd-agent-abuse` - musi znalezc sie w KORZENIU docelowego repozytorium GitHub. Tylko wtedy
> workflow'y w `.github/workflows/` zostana wykryte przez GitHub Actions, a sciezki takie jak
> `dotnet run --project src/AutoFixAgent` beda sie zgadzaly. Wlasnie to robi krok deploymentu przy
> publikowaniu do dedykowanego repozytorium demo.

1. Skonfiguruj w repozytorium GitHub (Settings -> Secrets and variables -> Actions) sekrety:
   `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`, `BEDROCK_MODEL_ID`.
2. Utworz Personal Access Token (classic, zakres `repo`) na koncie, ktore ma prawo tworzyc
   branche/commity/PR-y/etykiety w docelowym repozytorium, i dodaj go jako sekret repozytorium
   `AGENT_PAT`. To jest INNY token niz automatyczny `GITHUB_TOKEN` - jest to konieczne, bo GitHub
   Actions ma udokumentowane ograniczenie: zdarzenia (np. otwarcie PR, dodanie etykiety) utworzone
   za pomoca automatycznego `GITHUB_TOKEN` NIE wywoluja kolejnych workflow'ow (ochrona przed
   nieskonczona rekursja triggerow) - wylaczone sa tylko `workflow_dispatch`/`repository_dispatch`.
   Bez `AGENT_PAT`, `auto-merge-vulnerable.yml` i `defense-gate.yml` nigdy by sie nie uruchomily po
   PR-ze otwartym przez `AutoFixAgent`, co zabiloby klimaks obu demo (atak i obrone).
3. Wlacz "Allow auto-merge" w Settings -> General tego repozytorium (wymagane przez
   `gh pr merge --auto`).
4. Utworz nowe issue, kopiujac dokladnie tresc z `examples/poisoned-issue.md`.
5. Obserwuj zakladke Actions: najpierw uruchamia sie `Auto-fix issue (AI agent)`, potem (po
   otwarciu PR z etykieta `ai-generated`) `Auto-merge AI-generated PRs`.
6. Pokaz PR - w diffie widac nowy endpoint `/debug/env` z `Environment.GetEnvironmentVariables()`
   i wywolaniem `HttpClient`.
7. Bez wlaczonej obrony (patrz nizej), PR zostaje automatycznie zmergowany do `main`.

## Obrona - live

1. Ustaw w Settings -> Branches regule ochrony dla `main`, wymagajaca przejscia checka
   `scan-diff` (z workflow `defense-gate.yml`) przed merge.
2. Powtorz scenariusz ataku (nowe issue z tresci `examples/poisoned-issue.md`).
3. Pokaz, ze `defense-gate.yml` wykrywa wzorzec (HttpClient + Environment.GetEnvironmentVariables
   w tym samym diffie) i failuje required check - `auto-merge-vulnerable.yml` nie moze zmergowac
   PR, poniewaz wymagany check nie przeszedl.
4. Omow dodatkowe warstwy obrony spoza tego demo: brak auto-merge dla PR-ow oznaczonych jako
   AI-generated (wymog human review zawsze), sandboxing agenta (ograniczone uprawnienia tokena,
   brak dostepu do secrets produkcyjnych), secret scanning (np. gitleaks) jako dodatkowy required
   check, code owners dla plikow workflow.

## Testy (nie wymagaja AWS/GitHub credentials)

```bash
dotnet test src/VictimApi.Tests/VictimApi.Tests.csproj
dotnet test src/Defense.Tests/Defense.Tests.csproj
dotnet test src/AutoFixAgent.Tests/AutoFixAgent.Tests.csproj
```

`IModelClient` i `IRepoClient` sa zawsze wstrzykiwane jako parametry - prawdziwe implementacje
(`BedrockModelClient`, `OctokitRepoClient`) sa tworzone tylko w `src/AutoFixAgent/Program.cs`,
poza automatycznym pakietem testow.
