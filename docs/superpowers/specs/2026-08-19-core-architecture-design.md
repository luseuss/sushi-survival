# 스시왕국: 서바이벌 — 코어 아키텍처 & 슬라이스 1 설계

- 상태: 승인됨 (브레인스토밍 완료, 구현 계획 대기)
- 관련 문서: [CLAUDE.md](../../../CLAUDE.md) (기획서 v1.3 압축본), `게임기획서_v1.3.docx` (원본)
- 날짜: 2026-08-19

## 배경 및 목적

기획서(CLAUDE.md)에 정의된 스시왕국 서바이벌은 캐릭터 3종, 무기 3종(레벨 1~4),
몬스터 3종(잡몹/중형몹/보스), 웨이브 타임라인, 레벨업 선택, 증강 10종, 호감도 대화
2세트, 결과 화면 등 다수의 서브시스템으로 구성된다. 1개월·2~3인 개발이라는 제약과
"조정 가능한 밸런스"라는 요구사항을 고려해, 전체를 한 번에 구현하지 않고 **수직
슬라이스(slice) 단위로 단계적으로 확장**하는 전략을 취한다.

본 문서는 (1) 프로젝트 전반의 기술 아키텍처와 (2) 그 아키텍처를 검증하는 첫 번째
수직 슬라이스(슬라이스 1)의 범위를 정의한다. 슬라이스 2 이후(나머지 캐릭터/무기,
웨이브 타임라인, 증강, 호감도 대화, UI 화면 등)는 슬라이스 1이 동작을 검증한 뒤
별도 스펙/계획 사이클로 이어간다.

## 결정된 기술 스택

- Unity **2022.3 LTS**
- 입력: **새 Input System 패키지** (키보드+게임패드를 하나의 액션맵으로 통합, 추후
  게임패드/Steam 대응이 쉬움)
- 렌더 파이프라인: **Built-in 2D** (URP 2D 라이팅 불필요, 설정 단순화로 개발 속도 우선)
- 아키텍처: **데이터 기반 ScriptableObject + 중앙 매니저 + 오브젝트 풀링**

## 아키텍처

### 접근법 비교 (승인됨: A안)

| 안 | 설명 | 채택 여부 |
|---|---|---|
| A. SO 데이터 기반 + 중앙 매니저 + 풀링 | 무기/캐릭터/몬스터/증강 수치를 ScriptableObject로 분리, 중앙 시스템(GameManager/WaveManager/StatSystem/ObjectPool)으로 결합도 낮춤 | **채택** |
| B. MonoBehaviour 직접 필드 | 수치를 컴포넌트에 직접 하드코딩 | 기각 — 밸런스 반복 조정·재사용성 요구사항에 부적합 |
| C. ECS/DOTS | 고성능이지만 학습비용 큼 | 기각 — 이 프로젝트 규모(부스 데모)에 과함 |

### 폴더 구조

```
Assets/
  _Project/
    Scripts/
      Core/          # GameManager, WaveManager, StatSystem, ObjectPool
      Player/        # PlayerController, PlayerFacing, PlayerHealth
      Weapons/       # WeaponBase, EggFan, (추후) ShrimpRifle, NariClaw, Projectile
      Enemies/       # EnemyBase, EnemyAI, EnemySpawner
      Pickups/       # XPGem, MagnetPickup
      Data/          # ScriptableObject 클래스 정의
      UI/            # (2단계 이후) 레벨업 팝업, 결과화면 등
    Data/            # 실제 SO 에셋 인스턴스(.asset)
    Prefabs/
    Scenes/
  캐릭터/ 환경/       # 기존 아트 에셋(임포트 후 스프라이트 슬라이스 적용)
```

### 핵심 시스템 (슬라이스 1에서 구현)

- **GameManager**: 런 상태(진행중/승리/패배)와 경과 시간 타이머를 보유하는 씬 내
  단일 인스턴스. 슬라이스 1에서는 패배 조건(HP 0)만 처리.
- **StatSystem**: 엔티티(우선 플레이어)에 부착되는 컴포넌트. base값 + modifier
  리스트(가산/승산 조합)로 최종값을 계산하고, 스탯별 상한(cap)을 클램프한다.
  증강 10종과 호감도 대화 버프가 같은 캡 예산을 공유하므로 이 한 곳에서만
  클램프 처리한다(기획서 요구사항). 슬라이스 1에서는 골격만 만들고 실제 증강
  적용은 슬라이스 3에서 연결.
- **ObjectPool**: 몹/총알/젬이 공유하는 제네릭 풀링 유틸리티. `Get()`/`Release()`.
- **EnemySpawner**: 슬라이스 1에서는 웨이브 타임라인 없이 잡몹을 화면 밖 링에서
  일정 주기로 반복 스폰.
- **PlayerController + Facing 컴포넌트**: Input System 기반 이동, "정지 시 마지막
  이동 방향 유지" 로직을 공용 컴포넌트로 분리(기획서 요구사항 — 간장새우·이나리가
  이후 재사용).
- **WeaponBase(계란 양산 구현체)**: `WeaponData`에서 레벨별 수치를 읽어 쿨타임마다
  부채꼴 범위 내 몹에 다중 히트.
- **EnemyBase**: 체력/접촉데미지/단순 추적 AI(Transform 기반, NavMesh 미사용),
  사망 시 XP 젬 드롭 후 풀 반환.
- **XPGem**: 플레이어 근접 시 고정 반경으로 흡수 → GameManager에 경험치 전달.

### 데이터(ScriptableObject) 스키마

```csharp
// CharacterData.cs
- characterName, portraitSprite
- baseMoveSpeed, baseMaxHP
- weaponData            // WeaponData 참조
- animatorController / 스프라이트 시트 참조

// WeaponData.cs
- weaponName
- WeaponLevelStats[4]   // Lv1~4
    - damage, cooldown
    - range(radius), angle(부채꼴, 근접무기만)
    - pierceCount(관통 수, 원거리 무기만)
- projectilePrefab      // 원거리 무기만, null이면 근접

// MonsterData.cs
- monsterName, maxHP, contactDamage, moveSpeed
- xpGemDropType         // 기본 / 5XP / 10XP
- spriteSheet / animatorController

// AugmentData.cs (2단계에서 사용, 스키마는 슬라이스 1에서 미리 정의)
- statType              // enum: 공격력/공격속도/범위/체력/방어력/회복/이동속도/자석/경험치획득/부활
- valuePerPick, maxCap
```

기획서의 계란 Lv1~4 수치(데미지 8/10/12/15, 반경2.0~2.6·부채꼴120~150°, 쿨타임
1.2~1.0초)와 잡몹 수치(체력12, 접촉데미지5)는 코드에 하드코딩하지 않고 해당 SO
에셋의 인스펙터 필드에 입력한다.

## 슬라이스 1 범위 (완료 기준)

### 포함

1. 빈 씬 + 기존 환경 아트(바닥 스프라이트) 배치
2. 계란 캐릭터: WASD 이동, 정지 시 마지막 이동 방향 유지(좌우 반전), 대기/이동
   애니메이션 재생
3. 양산 무기: 자동 공격, Lv1 수치로 부채꼴 범위 내 다중 히트, 4프레임 애니메이션
   (접힘→펼치기 시작→최대 전개→접으며 회수)
4. 잡몹: 화면 밖 스폰 → 플레이어 추적 → 접촉 시 플레이어 데미지 → 체력 0 사망 →
   풀 반환
5. XP 젬(흰색) 드롭 → 플레이어 근접 흡수 → 누적 경험치 콘솔 로그로 확인
6. 플레이어 체력 0 → "GAME OVER" 콘솔 로그 (결과화면 UI는 2단계)

### 제외 (슬라이스 2 이후)

- 레벨업 3택 팝업 UI
- 나머지 캐릭터 2종(간장새우/이나리)과 그 무기
- 중형몹/보스, 웨이브 타임라인(시간대별 스폰 스케줄)
- 증강 10종 실제 적용 로직(스키마만 슬라이스 1에서 선반영)
- 호감도 대화 시스템(#1, #2)
- 결과 화면, 캐릭터 선택 화면 UI

## 에러 처리 / 엣지 케이스

- SO 데이터 미할당(null) 시 `WeaponBase`/`EnemyBase`는 `OnValidate` 또는 시작 시
  `Debug.LogError`로 명확히 알리고 기본값(0 데미지 등)으로 폴백하지 않는다 —
  밸런스 오류를 조용히 숨기지 않기 위함.
- 오브젝트 풀 고갈 시(동시 스폰 몹 수가 풀 크기를 초과) 풀은 자동으로 확장한다
  (초기 크기는 넉넉히, 예: 잡몹 100).
- 플레이어 체력이 음수로 내려가는 프레임에도 UI/로직은 0으로 클램프.

## 테스트 전략

- **수동 플레이테스트 중심**: 이동/공격/피격/사망/젬 획득을 Play 모드에서 직접
  확인. 뱀서라이크 특유의 실시간 밸런스 감각은 자동화보다 수동 테스트가 더
  실용적.
- **Edit Mode 유닛 테스트**: `StatSystem`의 modifier 합산·클램프 로직처럼
  MonoBehaviour/씬에 의존하지 않는 순수 로직에 한해 Unity Test Framework로 작성.

## 향후 단계 (참고용, 본 스펙 범위 아님)

- 슬라이스 2: 간장새우/이나리 캐릭터·무기 추가, 레벨업 3택 팝업, 중형몹/보스,
  웨이브 타임라인
- 슬라이스 3: 증강 10종 실제 적용(StatSystem 연결), 결과 화면
- 슬라이스 4: 호감도 대화 시스템(#1 필수, #2 should)
