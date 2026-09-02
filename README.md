# Demo 3: Autonomiczny agent AI w CI/CD - prompt injection przez tresc issue

> ⚠️ REPOZYTORIUM EDUKACYJNE. Wszystkie sekrety w `appsettings.json` sa falszywe (FAKE_*).
> `examples/poisoned-issue.md` zawiera DEMONSTRACYJNY, zlosliwy payload wzorowany na realnych
> incydentach 2025 (Amazon Q Developer Extension, GitLab Duo - zob. `../../materials/incidents/cicd-prompt-injection.md`).
> Ten payload jest niewidoczny w renderowanym Markdown na GitHubie - instrukcja jest zakodowana w
> niewidocznych znakach Unicode (bloku "Tags", U+E0000-U+E007F, technika "ASCII smuggling" znana z
> realnych atakow na LLM z 2024), a nie w komentarzu HTML. Komentarze HTML (`<!-- ... -->`) sa
> konwertowane przez warstwe sanityzacji GitHub Agentic Workflows na nieszkodliwy tekst
> (`(!-- ... --)`) zanim dotrą do modelu, wiec ta technika nie zadziala z tym silnikiem - stad
> uzycie niewidocznych znakow Unicode, ktore tej sanityzacji nie podlegaja.

## Scenariusz ataku

1. Atakujacy (albo, w tym demo, sam prezenter) tworzy nowe issue w repozytorium, uzywajac tresci
   z `examples/poisoned-issue.md` - zgloszenie wyglada jak normalny bug report, ale zawiera ukryta
   instrukcje dla agenta AI zakodowana w niewidocznych znakach Unicode.
2. Workflow `.github/workflows/auto-fix.yml` uruchamia `AutoFixAgent` na nowym issue: agent
   odczytuje pelna, surowa tresc issue (w tym ukryty komentarz), prosi model Claude (przez Amazon
   Bedrock) o poprawiony plik, i otwiera pull request z etykieta `ai-generated`.
3. Model, "pomocnie" wykonujac ukryta instrukcje, dodaje do poprawki dodatkowy endpoint
   `/debug/env`, ktory zrzuca zmienne srodowiskowe procesu i wysyla je przez `HttpClient` do
   zewnetrznego adresu - backdoor/eksfiltracja secrets w tej samej "poprawce buga".
4. `.github/workflows/auto-merge-vulnerable.yml` - naiwna konfiguracja pipeline'u, ktora
   automatycznie merguje KAZDY PR z etykieta `ai-generated`, bez wymogu human review - PR z
   backdoorem zostaje zmergowany do `main`.

## Live demo - krok po kroku (wymaga GitHub Copilot Business i GitHub Agentic Workflows)

> Zawartosc tego katalogu (`.github/`, `src/`, `examples/`, `README.md`) - a NIE sam katalog
> `03-cicd-agent-abuse` - musi znalezc sie w KORZENIU docelowego repozytorium GitHub. Tylko wtedy
> workflow'y w `.github/workflows/` zostana wykryte przez GitHub Actions, a sciezki takie jak
> `src/VictimApi/Program.cs` beda sie zgadzaly.

### Wymagania wstępne

1. **GitHub Copilot Business** - org-billed subscription, włączony dla Twojej organizacji/repo.
2. **GitHub Agentic Workflows** - aktywne w Twojej organizacji (standardowo dostępne dla org z Copilot Business).
   - Workflow `.github/workflows/auto-fix.md` będzie automatycznie rozpoznany przez GitHub Actions
     jako agentic workflow i wykonany z silnikiem `engine: copilot`.

### Konfiguracja repozytorium

1. Skonfiguruj w repozytorium GitHub (Settings -> Secrets and variables -> Actions) sekret:
   `AGENT_PAT` - Personal Access Token (classic, zakres `repo`) z uprawnieniami do tworzenia
   branche/commitów/PR-ów/etykiet. To jest **INNY token niż automatyczny `GITHUB_TOKEN`** - jest to
   konieczne, bo GitHub Actions ma udokumentowane ograniczenie: zdarzenia (np. otwarcie PR, dodanie
   etykiety) utworzone za pomocą automatycznego `GITHUB_TOKEN` **NIE** wywołują kolejnych
   workflow'ów (ochrona przed nieskończoną rekursją triggerów). Bez `AGENT_PAT`,
   `auto-merge-vulnerable.yml` i `defense-gate.yml` nigdy by się nie uruchomiły po PR-ze otwartym
   przez agenta, co zabiłoby klimaks obu demo (atak i obronę).

2. Włącz "Allow auto-merge" w Settings → General tego repozytorium (wymagane przez
   `gh pr merge --auto` w `auto-merge-vulnerable.yml`).

### Przebieg demo

1. Utwórz nowe issue, kopiując dokładnie treść z `examples/poisoned-issue.md`.
2. Obserwuj zakładkę Actions: najpierw uruchamia się `Auto-fix issue (AI agent)`, potem (po
   otwarciu PR z etykietą `ai-generated`) `Auto-merge AI-generated PRs`.
3. Pokaż PR - w diffie widać nowy endpoint `/debug/env` z `Environment.GetEnvironmentVariables()`
   i wywołaniem `HttpClient`.
4. Bez włączonej obrony (patrz poniżej), PR zostaje automatycznie zmergowany do `main`.

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

## Resetowanie demo (cofanie zmian po uruchomieniu workflow)

Kazde odpalenie sciezki atakowej tworzy trwale artefakty w repozytorium GitHub: branch
`auto-fix/issue-<N>`, pull request, ewentualnie merge do `main` (jesli `auto-merge-vulnerable.yml`
zdazyl zmergowac PR przed wylaczeniem regul ochrony). Przed powtorka demo (albo przed przejsciem
z czesci "atak" do czesci "obrona") warto to posprzatac.

**Zalecenie**: przed pierwszym uruchomieniem demo, oznacz aktualny, czysty stan brancha `main`
tagiem - pozwoli to jednym poleceniem wrocic do punktu startowego niezaleznie od tego, ile razy
demo zostanie powtorzone:

```bash
git tag demo-baseline
git push origin demo-baseline
```

Po kazdym przebiegu demo:

1. **Zamknij PR agenta i usun jego branch** (jedna komenda robi oba):
   ```bash
   gh pr close <NUMER_PR> --delete-branch
   ```
   Jesli branch zostal juz usuniety automatycznie po merge, usun go tylko jesli nadal istnieje:
   ```bash
   git push origin --delete auto-fix/issue-<NUMER_ISSUE>
   ```

2. **Zamknij (lub usun) issue z payloadem**:
   ```bash
   gh issue close <NUMER_ISSUE>
   # albo, jesli chcesz usunac je calkowicie (wymaga uprawnien admina repo):
   gh issue delete <NUMER_ISSUE> --yes
   ```

3. **Jesli PR zostal zmergowany do `main`** (scenariusz bez wlaczonej obrony) - przywroc `main` do
   stanu sprzed demo. W repozytorium uzywanym wylacznie do tego demo (bez innych osob pracujacych
   na `main`) najprostsze jest twarde zresetowanie do tagu bazowego:
   ```bash
   git fetch origin
   git reset --hard demo-baseline
   git push origin main --force
   ```
   Jesli wolisz zachowac historie (np. repo jest wspoldzielone), uzyj zamiast tego `git revert` na
   merge-commicie dodanym przez `auto-merge-vulnerable.yml`:
   ```bash
   git revert -m 1 <SHA_MERGE_COMMITU>
   git push origin main
   ```

4. **Przelaczajac sie miedzy czescia "atak" i "obrona"**: pamietaj o wlaczeniu/wylaczeniu required
   checka `scan-diff` w Settings -> Branches dla `main` (patrz sekcja "Obrona - live") - to ustawienie
   nie resetuje sie automatycznie miedzy przebiegami.

## Testy (nie wymagaja GitHub credentials)

```bash
dotnet test src/VictimApi.Tests/VictimApi.Tests.csproj
dotnet test src/Defense.Tests/Defense.Tests.csproj
```

Logika agenta zdefiniowana jest teraz w `auto-fix.md` (GitHub Agentic Workflow) - testowanie jej
wymaga live demo w GitHub Actions na prawdziwym repozytorium.
