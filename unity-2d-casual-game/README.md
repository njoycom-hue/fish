# Tap Runner — Unity 2D 캐주얼 게임 (Android)

원터치 점프로 장애물을 피하고 코인을 모으는 엔드리스 러너입니다.
아트/사운드 에셋 없이도 **클론 → 씬 생성 → Play** 만으로 바로 굴러가도록 구성했습니다.

- 엔진: Unity **6000.0.23f1** (Unity 6 LTS 계열)
- 타깃: **Android** (세로 고정, IL2CPP, ARMv7 + ARM64, minSdk 24)
- 패키지 ID: `com.duruone.casual2d`
- 렌더링: 빌트인 렌더 파이프라인 + `Unlit/Color` 단색 쿼드

---

## 1. 시작하기

```bash
git clone <이 저장소 주소>
```

1. Unity Hub → **Add project from disk** 로 이 폴더를 추가합니다.
   (설치된 Unity 버전이 다르면 `ProjectSettings/ProjectVersion.txt` 를 본인 버전으로 바꾸거나,
   Hub 가 안내하는 업그레이드를 그대로 진행하면 됩니다.)
2. 에디터가 열리면 상단 메뉴에서 **Tools ▸ 2D Runner ▸ 샘플 씬 생성** 실행
   → `Assets/Scenes/Main.unity` 와 `Assets/Prefabs/*.prefab` 이 만들어지고 빌드 설정에 등록됩니다.
3. **Tools ▸ 2D Runner ▸ Android 설정 적용** 실행 (패키지명·해상도·IL2CPP 등 일괄 적용)
4. Play ▶ — 화면을 탭(또는 Space / 마우스 좌클릭)하면 시작, 공중에서 한 번 더 누르면 2단 점프.

> 씬과 프리팹을 코드로 생성하는 이유: 손으로 만든 `.unity`/`.prefab` YAML 은 병합 충돌이
> 잦고 리뷰가 불가능합니다. 생성기(`Assets/Editor/SceneBootstrap.cs`)를 저장소의 진실로 두면
> 누구나 동일한 씬을 재현할 수 있고, 씬 구성 변경이 diff 로 남습니다.

---

## 2. 폴더 구조

```
Assets/
├─ Scripts/                 런타임 코드 (어셈블리: Game)
│  ├─ Core/
│  │  ├─ GameManager.cs     상태 머신(Ready/Playing/Paused/GameOver), 주행 속도, 점수 소유
│  │  ├─ GameState.cs
│  │  ├─ DifficultyCurve.cs 경과 시간 → 속도/생성 간격 (순수 클래스, 테스트 대상)
│  │  ├─ ScoreService.cs    거리 + 코인 점수 집계 (순수 클래스, 테스트 대상)
│  │  ├─ SaveSystem.cs      PlayerPrefs + JSON 최고점수/코인/설정 저장
│  │  ├─ ObjectPool.cs      제네릭 컴포넌트 풀 + IPoolable
│  │  └─ AudioManager.cs    클립이 비어 있어도 안전하게 동작하는 SFX/BGM
│  ├─ InputSystem/          IInputSource 추상화 + 터치/키보드 구현
│  ├─ Gameplay/
│  │  ├─ PlayerController2D.cs  코요테 타임 · 점프 버퍼 · 가변 점프 높이
│  │  ├─ Scroller.cs / GroundScroller.cs  월드 스크롤 및 무한 바닥
│  │  ├─ Spawner.cs         난이도 기반 장애물·코인 생성 (풀 사용)
│  │  └─ Obstacle.cs / Coin.cs
│  └─ UI/HudController.cs   점수 HUD, 시작·일시정지·결과 패널
├─ Editor/                  (어셈블리: Game.Editor, 에디터 전용)
│  ├─ SceneBootstrap.cs     샘플 씬·프리팹·머티리얼 생성기
│  ├─ ProjectConfigurator.cs Android 플레이어 설정 일괄 적용
│  └─ BuildScript.cs        APK/AAB 배치 빌드 진입점
└─ Tests/EditMode/          NUnit 테스트 (점수, 난이도 곡선, 오브젝트 풀)
```

핵심 원칙: **게임 규칙은 MonoBehaviour 밖에 둡니다.** `DifficultyCurve`, `ScoreService`,
`ObjectPool` 은 씬 없이 테스트되고, MonoBehaviour 는 이들을 조립하는 얇은 껍데기입니다.

---

## 3. 게임 규칙

| 항목 | 값 | 위치 |
|---|---|---|
| 시작 속도 → 최고 속도 | 5 → 14 units/s (90초에 걸쳐) | `DifficultyCurve` |
| 장애물 간격 | 1.6초 → 0.62초 (75초에 걸쳐) | `DifficultyCurve` |
| 점수 | 이동 거리 1 unit = 1점, 코인 1개 = 10점 | `ScoreService` |
| 점프 | 2단 점프, 코요테 타임 0.1초, 입력 버퍼 0.12초 | `PlayerController2D` |
| 난이도 후반 | 진행도 0.55 이상에서 장애물 2개 동시 배치 | `Spawner` |

밸런스는 전부 인스펙터에 노출되어 있으니 값만 바꿔 가며 조정하면 됩니다.

---

## 4. 테스트

에디터: **Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All**

CLI:

```bash
Unity -batchmode -projectPath . -runTests -testPlatform EditMode \
      -testResults ./TestResults.xml -logFile -
```

---

## 5. 빌드

에디터 메뉴:

- **Tools ▸ 2D Runner ▸ Android APK 빌드** — 기기 테스트용
- **Tools ▸ 2D Runner ▸ Android AAB 빌드 (스토어용)** — Google Play 업로드용

CLI / CI:

```bash
Unity -quit -batchmode -projectPath . \
      -executeMethod Game.EditorTools.BuildScript.BuildAndroid -logFile -
```

빌드 스크립트가 읽는 환경 변수:

| 변수 | 설명 |
|---|---|
| `BUILD_AAB` | `true` 면 AAB, 아니면 APK |
| `BUILD_VERSION` | `PlayerSettings.bundleVersion` (예: `1.0.3`) |
| `BUILD_NUMBER` | `bundleVersionCode` (정수, 업로드마다 증가해야 함) |
| `ANDROID_KEYSTORE_PATH` / `_PASS` | 릴리스 키스토어 경로·비밀번호 |
| `ANDROID_KEYALIAS_NAME` / `_PASS` | 키 별칭·비밀번호 |

키스토어 환경 변수가 없으면 디버그 서명으로 빌드됩니다(기기 설치는 되지만 스토어 업로드는 불가).
**키스토어 파일과 비밀번호는 절대 커밋하지 마세요** — `.gitignore` 에 `*.keystore`, `*.jks` 가 이미 들어 있습니다.

### GitHub Actions

`.github/workflows/unity-android.yml` 이 EditMode 테스트 → Android 빌드를 실행합니다.
동작시키려면 저장소 시크릿이 필요합니다:

- `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` — [GameCI 라이선스 활성화](https://game.ci/docs/github/activation) 참고
- (선택) `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS`

시크릿을 넣기 전에는 워크플로가 라이선스 단계에서 실패합니다 — 정상입니다.

---

## 6. 다음 단계

1. **한글 UI 폰트** — 현재 HUD는 Unity 내장 폰트를 쓰기 때문에 한글 글리프가 없습니다.
   TextMeshPro(`com.unity.textmeshpro` 또는 Unity 6 내장 `com.unity.ugui`의 TMP)를 추가하고
   한글 폰트(예: Pretendard, 나눔고딕)를 **Font Asset Creator** 로 구운 뒤
   `HudController` 의 `Text` 를 `TMP_Text` 로 교체하면 됩니다.
   그 전까지 UI 문구는 의도적으로 ASCII(`TAP TO START`, `GAME OVER`)로 두었습니다.
2. **아트 교체** — `SceneBootstrap` 이 만드는 단색 쿼드를 스프라이트로 바꾸고,
   `Assets/Art/` 아래 이미지를 넣습니다(`.gitattributes` 에 LFS 규칙이 이미 있습니다).
3. **사운드** — `AudioManager` 인스펙터의 클립 4칸(jump/coin/crash/music)을 채우면 바로 재생됩니다.
4. **광고·통계** — 필요 시 Unity Ads / Analytics 패키지를 붙이고,
   개인정보처리방침 링크는 기존 `link.duruone.com` 사이트를 재사용할 수 있습니다.
