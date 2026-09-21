# 우리집 가계부

부부 공용 가계부 웹앱. Blazor Server + SQLite + MudBlazor + Fly.io 배포.

---

## 기술 스택

| 항목 | 내용 |
|---|---|
| 프레임워크 | ASP.NET Core 8 Blazor Server (Interactive Server) |
| UI | MudBlazor 9 |
| DB | SQLite (EF Core 8, `IDbContextFactory`) |
| 배포 | Fly.io (region: nrt, 1GB RAM) |
| 저장소 | Persistent Volume `/data` 마운트 |

---

## 변경 이력

### 2026-09-21

#### 영수증 기능 제거
- `ReceiptUpload.razor`, 영수증 업로드 API, JS `triggerInput`, CSS 전부 삭제
- `Entry.ReceiptUrl` 컬럼 제거 마이그레이션 추가 (`20260921000000_RemoveReceiptUrl`)

#### 페이지 이동 시 스크롤 플래시 수정
- **원인**: `MainLayout.razor`의 `OnLocationChanged`가 네비게이션 시작 즉시 현재 페이지를 맨 위로 스크롤 → 신규 페이지 DOM이 그려지기 전에 현재 페이지 상단이 보이는 현상
- **수정**: `MainLayout`에서 `OnLocationChanged` 핸들러 완전 제거 (`NavigationManager` inject, `IDisposable` 구현 포함)
- 편집 페이지는 `EntryEditPage.OnAfterRenderAsync`에서 자체 스크롤 처리

#### Fly.io 배포 후 첫 로드 지연 수정
- **원인**: JIT 워밍업 완료 전에 Fly.io가 신규 머신으로 트래픽 라우팅
- **수정**: `/health` 엔드포인트 추가 — 워밍업 미완료 시 503, 완료 후 200
- `fly.toml`에 `[[http_service.checks]]` 추가 (`grace_period=3s`, `interval=2s`, `path=/health`)
- 효과: 배포 후 Fly.io가 구 머신으로 계속 라우팅 → 워밍업 완료 후 신 머신 전환 → 첫 사용자도 빠른 응답

---

### 2026-09-20

#### 할부 완납 판정 오류 수정
- **원인**: `RemainingMonths` 계산 시 당월 선택 시 0 반환 → `IsCompleted = true` (잘못된 완납 처리)
- **수정**: `+1` 추가로 당월 = 1개월 남음으로 처리

#### 할부 남은 기간 표시 개선
- `RemainingMonths == 1` (마지막 납부월) → `D-{일수}` 형식으로 표시
- `RemainingMonths >= 2` → `{N}개월` 형식 유지
- `RemainingDays` 계산 프로퍼티 추가 (`Installment.cs`)

#### JIT 워밍업 — 첫 로드 지연 개선
- 앱 시작 후 1초 대기 → 자기 자신에게 HTTP GET 요청
- Blazor SSR 렌더링 파이프라인 전체 JIT 컴파일 완료
- Home.razor의 6개 DB 쿼리 `Task.WhenAll` 병렬화

#### 홈 화면 로딩 스피너
- `_initialized = false` 동안 `MudProgressLinear` 스피너 표시

#### 달력 DatePicker 기본 뷰 수정
- `OpenTo="OpenTo.Month"` → `OpenTo="OpenTo.Date"` (날짜 선택 화면부터 열림)

#### 브라우저 탭 제목 업데이트 수정
- `App.razor`의 `<HeadOutlet>` → `<HeadOutlet @rendermode="InteractiveServer" />`
- 페이지 이동 시 `<PageTitle>`이 실시간으로 탭 제목 반영

#### 토스트(Snackbar) 설정 통일
- 위치: 화면 상단 중앙 (`mud-snackbar-location-top-center`)
- 표시 시간: 2초 (`VisibleStateDuration = 2000`)
- 전환 애니메이션: 200ms

#### 편집 페이지 스크롤 위치
- `EntryEditPage.OnAfterRenderAsync`에서 `window.scrollTo(0, 0)` 호출
- 편집 화면 진입 시 항상 상단에서 시작

#### 스크롤 애니메이션 제거
- `html, body`에서 `scroll-behavior: smooth` 제거
- `blazor:navigating` 이벤트에서 `scrollBehavior: auto` 강제 설정

#### InstallmentDialog 크기 축소
- 다이얼로그 min-width 420→300px, max-width 480→400px
- 미리보기 영역 패딩·폰트 축소 (저장 버튼 잘림 현상 수정)

---

## 배포

```bash
fly deploy
```

- `min_machines_running = 1` → 항상 1대 유지 (콜드 스타트 없음)
- `/health` 엔드포인트로 워밍업 완료 후 트래픽 수신
- DB는 `/data/household.db` (Persistent Volume)
