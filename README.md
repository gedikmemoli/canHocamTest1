# Dijital Oyunlarda Yapay Zeka — Hafta 3, 4, 5, 6 ve 7 Tam Uygulama ve Ödev Raporu

Bu proje, **Hafta 3, Hafta 4, Hafta 5, Hafta 6 ve Hafta 7** ders notlarındaki tüm ders içi uygulamaları, laboratuvar deneylerini, ödevleri ve ek görevleri (+10 puanlık bonuslar dahil) **aşama aşama** barındıracak şekilde yapılandırılmıştır.

---

## 🚀 Projede Aşamalar Arası Geçiş Nasıl Yapılır?

Projeyi Unity ile açtığınızda (`SetupTerrainAndScene.cs` otomatik çalışır veya üst menüden **`Tools > Hafta 3-7 Tüm Aşamaları ve Sahneleri Kur (Tam Kurulum)`** tıklanabilir):

1. **Oyun İçinde (Play Modunda) Aşama Değiştirme:**
   - Ekranın sağ üst köşesindeki **"📚 Ders & Ödev Aşamaları (F1-F5)"** panelinden veya klavyeden **`F1` – `F5`** tuşlarına basarak haftalar arasında anında geçiş yapabilirsiniz:
     - **`F1` — Hafta 3:** Arazi, Girdi, Hareket (`Oyuncu_CC` & `Oyuncu_RB`) ve **Gözcü Ödevi**
     - **`F2` — Hafta 4:** Arazide Labirent, NavMesh vs Kendi A*'ımız, Duvar Düzenleme ve **Dağ Yolu Ödevi (`TerrainGrid`)**
     - **`F3` — Hafta 5:** 20×20 Arena, Waypoint Grafı, `GraphAStar` ve **Köşe Noktaları Ödevi (`K1-K4`, Ping-Pong, Yol Yumuşatma)**
     - **`F4` — Hafta 6:** NavMesh Alan Maliyetleri (`Camur`, `SigSu`), `SlidingDoor`, `NavMeshLink` Parabolik Atlama ve **Kestirme mi Güvenli Yol mu Ödevi**
     - **`F5` — Hafta 7:** Sonlu Durum Makineleri (FSM - `Devriye`, `Kovala`, `Ara`, `KapıdaBekle`) ve **Sesi Duyan Bekçi Ödevi (`Araştır` + `MakeNoise`)**
2. **Ayrı Sahneler Olarak Açma (`Assets/Scenes/`):**
   - `Assets/Scenes/SampleScene.unity` (Tüm haftalar bir arada, F1-F5 ile geçişli)
   - `Assets/Scenes/Hafta3_Gozcu.unity`
   - `Assets/Scenes/Hafta4_Uygulama.unity`
   - `Assets/Scenes/Hafta5_Waypoint.unity`
   - `Assets/Scenes/Hafta6_NavMesh.unity`
   - `Assets/Scenes/Hafta7_FSM.unity`

---

## 📌 Hafta 3 — 3B Dünya: Arazi, Girdi, Hareket ve Gözcü Ödevi

### Uygulanan Bileşenler ve Görevler
- **Arazi (`Terrain_AnaArazi`):** 200×50×200 m boyutunda, `(-100, 0, -100)` konumunda; tepe, düzlük, 45°'den dik yamaç, 2 doku katmanı (`CimenKatmani`, `KayaKatmani`) ve 16 ağaç prefab'ı (`AgacPrefab`).
- **Arazi Sondası (`AraziSondasi.cs`):** `SampleHeight` ve `GetInterpolatedNormal` ile zemin yüksekliğini ve eğim açısını ($\alpha = \arccos(n_y)$) hesaplar, yürünebilirse yeşil, dik yamaçta kırmızı normal çizgisi çizer.
- **New Input System (`GirdiOkuyucu.cs`, `Hareket.cs`):** `Move`, `Look`, `Jump`, `Sprint` eylemlerini okur ve kameranın düzleştirilmiş ileri/sağ vektörlerine göre yön hesaplar (`ClampMagnitude` ile çapraz hızlanma engellenir).
- **İki Karakter (`CCHareket.cs` & `RBHareket.cs`):**
  - `Oyuncu_CC` (Euler adımıyla yerçekimi, $v_0 = \sqrt{2gh}$ zıplama) ve `Oyuncu_RB` (`FixedUpdate`, `ForceMode.VelocityChange`, `SphereCast` zemin testi). **`Tab`** tuşuyla iki karakter arasında canlı geçiş yapılır.
- **Takip Kamerası (`TakipKamerasi.cs`):** `LateUpdate`'te $t = 1 - e^{-k \Delta t}$ formülüyle kare hızından bağımsız yumuşatma.
- **Ödev — Gözcü (`Gozcu.cs`) + Ek Görevler (+10 Puan):**
  - `Vector3.Angle` kullanmadan nokta çarpımla yatay görüş konisi ve dikey açı sınırı (+3 puan).
  - `Physics.Linecast` ile arazi ve ağaç arkası saklanma kontrolü (`Player` katmanı hariç).
  - Oyuncuyu görmüyorsa yeşil, görüyorsa kırmızı; çapraz çarpım (`Vector3.Cross(ileri, hedef).y`) ile sağ/sol konsol çıktısı.
  - ±60° arasında 30°/s hızla tarama (+4 puan) ve oyuncuyu görünce takip edip 2 sn kaybedince taramaya dönme (+3 puan).

### Hafta 3 Laboratuvar Karşılaştırma Tablosu
| Deneme | `Oyuncu_CC` (`CharacterController`) | `Oyuncu_RB` (`Rigidbody`) |
| :--- | :--- | :--- |
| **0,25 m basamağa çıkabildi mi?** | Evet (`stepOffset = 0.3m` sayesinde takılmadan çıktı) | Hayır, kapsül alt yüzeyi çarpıp takıldı (zıplamak gerekir) |
| **0,5 m basamağa çıkabildi mi?** | Hayır (`0.5m > stepOffset`, zıplayarak çıkabildi) | Hayır, zıplayarak çıkabildi |
| **Dik yamaçta (>45°) ne oldu?** | `slopeLimit = 45°` nedeniyle yukarı tırmanamadı | Yukarı çıkarken yavaşlayıp aşağı doğru kaydı |
| **Küpü (`ItilebilirKup`) itebildi mi?** | Doğrudan fiziksel olarak itmez (`OnControllerColliderHit` ile elle kuvvet uygulandı) | Doğal fizik çarpışmasıyla kendiliğinden itti |
| **Tepeden aşağı koşarken zeminde kaldı mı?** | `dikeyHiz = -2f` bastırma hızı sayesinde zeminde kaldı | Yüksek hızda eğim değişiminde hafifçe havalanıp sıçradı |
| **Hareket hissi (bir cümle)** | Kesin, anında tepki veren, kaymasız kontrol hissi. | İvmelenme ve kütle hissi olan, fiziksel olarak doğal hareket. |

---

## 📌 Hafta 4 — Arazide Labirent, NavMesh vs A* ve Dağ Yolu Ödevi

### Uygulanan Bileşenler ve Görevler
- **`ISearchGrid.cs`, `PathMarker.cs`, `MinHeap.cs`, `Frontiers.cs`:** DFS (`Stack`), BFS (`Queue`), Dijkstra ve A* (`MinHeap`) veri yapıları.
- **`MazeGenerator.cs`:** Araziden `SampleHeight` ile yükseklik okuyan (`CellToWorld`), eğimli zeminde duvarların altında boşluk kalmaması için `wallSink = 1` ile duvar diken ve sağ tıkla `ToggleWall` yapan 31×31 labirent.
- **`NavMeshRefresher.cs` & `NavPathDebug.cs`:** Labirent hacmini (`CollectObjects.Volume`) çalışma anında pişirir (`BuildNavMesh`) ve duvar değiştiğinde arka planda günceller (`UpdateNavMesh`).
- **`GridPathfinder.cs`, `PathLine.cs`, `PathFollower.cs`:** `FindPath` ve `OnPathFound` olayıyla bulunan yolu arazi üzerinde sarı `LineRenderer` ile çizer ve `CharacterController` ajanı noktadan noktaya yürütür.
- **`ClickToGoal.cs`, `WallEditor.cs`, `Replanner.cs`, `CameraModes.cs`:** Sol tıkla hem NavMesh ajanına hem kendi A* ajanımıza hedef verir; sağ tıkla duvar ekler/kaldırır ve kalan yol kapandıysa anında yeniden planlar; `C` tuşuyla tepe ve takip kamerası arasında geçiş yapar.
- **Ödev — Dağ Yolu (`TerrainGrid.cs`):** Arazi eğimini $\alpha = \arccos(n_y)$ ile hesaplar, dik yamaçları (`>42°`) yürünemez yapar ve eğime göre hücre maliyeti (`CellCost`) vererek `PathFollower`'ın dik yerlerde yavaşlamasını sağlar. (HUD'daki butonla Labirent ve Dağ Yolu modu arasında geçiş yapılabilir.)

---

## 📌 Hafta 5 — Waypoint'ler, Graflar ve Köşe Noktaları Ödevi

### Uygulanan Bileşenler ve Görevler
- **`Waypoint.cs` & `WaypointGraph.cs`:** `W0..W8` düğümleri arasında `maxEdgeLength = 8f` ve `Physics.Linecast` (`Obstacle` katmanı) ile otomatik kenar kurulumu (`BuildEdges`), görünen en yakın düğüm (`Nearest`) ve `A* Testi` (`TestPath`).
- **`GraphAStar.cs`:** Komşuluk listesi ve Öklid uzaklığı sezgiseliyle genel graf üzerinde A* ve Dijkstra (`useHeuristic = false`).
- **`NpcWaypointGuard.cs`:** `Patrol`, `Chase`, `Return` durumlarıyla devriye ve A* takip.
- **Ödev — Köşe Noktaları (Bölüm 10) + Ek Görev (+10 Puan):**
  - **Görev 1:** U engelinin dış köşelerine `K1 (6, 0.5, 4)`, `K2 (14, 0.5, 4)`, `K3 (14, 0.5, 16)`, `K4 (6, 0.5, 16)` eklendi (13 düğüm, 18 yönsüz kenar).
  - **Görev 3:** `pingPong` (Gidip gelme: `0 1 2 ... 7 6 5 ... 0`) modu eklendi.
  - **Ek Görev (+10):** `SmoothPath()` ile görüş hattı tabanlı yol yumuşatma (`PlanTo` sonunda ara düğümleri atlama) eklendi.

### Hafta 5 Ödev Görev 2 — Ölçüm Tablosu (`TestBaşlangıç (6.5, 0.5, 13.5)` → `TestHedef (17.5, 0.5, 9.5)`)
| Graf | Düğüm | Kenar | Yol (düğümler) | A* genişletilen | Dijkstra genişletilen | Uzunluk (m) |
| :--- | :---: | :---: | :--- | :---: | :---: | :---: |
| **Halka (`K1–K4` kapalı)** | 9 | 9 | `W8 → W7 → W0 → W1 → W2 → W3` | 8 | 9 | **38,9 m** |
| **Halka + Köşeler (`K1–K4` açık)** | 13 | 18 | `K4 → W5 → K3 → W3` | 5 | 12 | **22,6 m** |

### Hafta 5 Ödev Görev 4 — Yorum (Üç Cümle)
1. **Yol neden kısaldı?** Görünürlük noktaları (points of visibility) fikrine göre en kısa yollar engellerin dış köşelerini sıyırarak geçtiği için, `K1–K4` köşe düğümleri NPC'nin önce U'nun içine (`W8`) girip geniş dış halkayı (`W7 → W0`) dolaşması yerine doğrudan üst dış köşeden (`K4 → W5 → K3`) dönmesini sağlayarak yolu 38,9 m'den 22,6 m'ye düşürmüştür.
2. **Genişletilen düğüm sayısı nasıl değişti?** Graf 9 düğümden 13 düğüme çıkmasına rağmen A*'ın genişlettiği düğüm sayısı 8'den 5'e düşmüştür; çünkü köşe düğümleri hedefe (`W3`) doğru Öklid sezgiselini ($h$) büyüütmeden doğrudan ilerleyen düşük $f = g + h$ maliyetli bir koridor açarak A*'ın alt kolu taramasını önlemiştir.
3. **Izgaradaki 19 m'lik yolla karşılaştırma:** 400 düğümlü ızgarada 71 düğüm genişletilerek bulunan 19 m'lik yola kıyasla, iyi yerleştirilmiş yalnızca 13 düğümlü bir görünürlük grafı 14 kat daha az arama adımıyla (5 genişletme) ızgara yoluna çok yakın (22,6 m; yol yumuşatmayla ~19,5 m) kalitede bir rota üretmiştir.

---

## 📌 Hafta 6 — Navigasyon Ağları (NavMesh) ve Kestirme mi Güvenli Yol mu Ödevi

### Uygulanan Bileşenler ve Görevler
- **Sahne Öğeleri:** 20×20 arenada `Camur` (`NavMeshModifierVolume`, User 3 alanı), `SigSu` (Ödev için 2. özel alan tipi, User 4 alanı), `Kapi` (`NavMeshModifier` Ignore From Build + `NavMeshObstacle` Carve + `SlidingDoor.cs` `K` tuşu) ve `AtlamaBaglantisi` (`NavMeshLink` Jump alanı + `JumpLinkTraverser.cs` parabolik atlayış: $p(t) = (1-t)A + tB + 4ht(1-t)\hat{y}$).
- **Deney ve Ölçüm (`AreaCostToggle.cs`, `PathDebugDraw.cs`, `NpcNavChase.cs`):**
  - `M` tuşuyla çamur maliyetini (`1 → 2 → 5 → 10`) döndürme, `J` tuşuyla atlama bağlantısını açıp kapatma, `K` tuşuyla kapıyı açıp kapatma.
  - Sol üst ekranda köşe sayısı, kalan yol uzunluğu, alan maliyetli toplam ve `PathStatus` (`PathComplete` / `PathPartial`) gösterimi.
  - **Ek Görev (+10 Puan):** `NpcNavChase.cs` içinde `NavMesh.SamplePosition` ile ajanın bastığı alan sorgulanır; `Camur` üzerinde hız yarıya (`0.5x`), `SigSu` üzerinde `0.65x` değerine düşürülür.

### Hafta 6 Ödev Rapor Özeti (Üç Rota ve Maliyet Eşiği)
- **Rota C (Atlama Bağlantısı açıkken):** Uzunluk $\approx 12{,}52\text{ m}$ (3 m Jump bağlantısı, maliyet çarpanı 2) $\Rightarrow g(C) \approx 15{,}52$. En ucuz rota olduğu için bağlantı açıkken NPC duvarın üstünden parabol çizerek atlar.
- **Rota A (Üst / Çamur Rotası, bağlantı kapalıyken):** Uzunluk $\approx 16{,}21\text{ m}$ (6 m'si `Camur` alanında) $\Rightarrow g(A) = 10{,}21 + 6c_{\text{çamur}}$.
- **Rota B (Alt / Kapı ve Sığ Su Rotası, bağlantı kapalı, kapı açıkken):** Uzunluk $\approx 22{,}40\text{ m}$. Çamur maliyeti $c_{\text{çamur}} > 2{,}03$ olduğunda (örneğin `M` tuşuyla $c = 5$ veya $10$ seçildiğinde) $g(A) = 40{,}21 > g(B)$ olduğu için NPC üstteki çamurdan kaçınıp alttaki kapıdan dolaşır. Kapı `K` tuşuyla kapatılırsa `Carve` ağı oyar ve NPC yüksek maliyete rağmen tekrar açık olan üst rotaya döner.

---

## 📌 Hafta 7 — Sonlu Durum Makineleri (FSM) ve Sesi Duyan Bekçi Ödevi

### Uygulanan Bileşenler ve Görevler
- **`State.cs` & `StateMachine.cs`:** `Current`, `Previous` ve `Global` durumları yöneten, geçişlerde her zaman sırasıyla `Previous = Current` $\rightarrow$ `Current.Exit()` $\rightarrow$ `Current = next` $\rightarrow$ `Current.Enter()` çalıştıran ve her geçişi `[{Time.time:0.0} sn] Eski → Yeni` biçiminde konsola yazan jenerik durum makinesi.
- **`Guard.cs` & `GuardStates.cs` (5 Durum + 1 Global Kural):**
  1. **`GuardGlobal`:** Her karede mevcut durumdan önce çalışır; `CanSeePlayer()` doğruysa `lastSeenPosition` günceller ve `Kovala`'ya geçirir.
  2. **`Devriye` (`GuardPatrol` - Yeşil):** `P0 → P1 → P2 → P3` noktalarını `2 m/sn` hızla gezer; kapalı kapıya `2.5 m`'den fazla yaklaşırsa `KapıdaBekle`'ye geçer.
  3. **`Kovala` (`GuardChase` - Kırmızı):** `Enter`'da hızı `4 m/sn` yapar, `Exit`'te `2 m/sn`'ye geri alır. Oyuncu `1 sn` boyunca görünmezse `Ara`'ya geçer.
  4. **`Ara` (`GuardSearch` - Sarı):** Son görülen noktaya gider, varınca `5 sn` boyunca `90°/sn` hızla dönerek bakınır; süre bitince `Devriye`'ye döner.
  5. **`KapıdaBekle` (`GuardWaitAtDoor` - Gri):** `Enter`'da `Agent.isStopped = true` yapar, kapı açılınca (`door.IsOpen`) `RevertToPreviousState()` ile geldiği duruma (`Devriye`, `Ara` veya `Araştır`) döner, `Exit`'te `Agent.isStopped = false` yapar.
  6. **Ödev — 5. Durum: `Araştır` (`GuardInvestigate` - Mor/Eflatun) & `PlayerNoiseMaker.cs`:**
     - `InputSystem_Actions` içerisine **`MakeNoise`** eylemi (`Keyboard: N`) eklendi.
     - Oyuncu `N` tuşuna bastığında `Guard.HearNoise(Vector3 position)` çağrılır.
     - Mesafe $\le 8\text{ m}$ ise (duvarlar sesi kesmez) ve bekçi `Devriye` veya `Ara` durumundaysa (`Kovala` durumundayken sesi görmezden gelir) **`Araştır`** durumuna geçer.
     - `Araştır` durumunda bekçi mor/eflatun renge bürünür, sesin geldiği noktaya gider, `3 sn` dönerek bakınır ve `Devriye`'ye döner (bu sırada oyuncuyu görürse `GuardGlobal` ek kod gerekmeden onu `Kovala`'ya geçirir).

### Hafta 7 Ödev Teslimi — Beş Durumlu Durum Diyagramı ve Geçiş Tablosu

```mermaid
stateDiagram-v2
    [*] --> Devriye : Başla
    note right of Kovala : Global Kural (her karede önce çalışır):\nOyuncu görüldü -> Kovala (Kırmızı, 4 m/sn)

    Devriye --> KapıdaBekle : Kapı kapalı ve < 2.5m
    Devriye --> Araştır : Ses duyuldu (N tuşu, <= 8m)

    Kovala --> Ara : 1 sn oyuncu görünmedi

    Ara --> KapıdaBekle : Kapı kapalı ve < 2.5m
    Ara --> Araştır : Ses duyuldu (N tuşu, <= 8m)
    Ara --> Devriye : Son görülen noktada 5 sn bakındı

    Araştır --> KapıdaBekle : Kapı kapalı ve < 2.5m
    Araştır --> Devriye : Ses noktasında 3 sn bakındı

    KapıdaBekle --> ÖncekiDurum : Kapı açıldı (Devriye / Ara / Araştır)
```

| Mevcut Durum | Koşul / Olay | Sonraki Durum |
| :--- | :--- | :--- |
| **Herhangi biri (`Kovala` hariç)** | Oyuncu görüldü (`CanSeePlayer() == true`, Global durum) | **Kovala** (Kırmızı) |
| **Devriye** (Yeşil) | Kapı kapalı ve kapıya $2{,}5\text{ m}$'den yakın (`DoorBlocksMe()`) | **KapıdaBekle** (Gri) |
| **Devriye** (Yeşil) | Oyuncu `N` tuşuyla ses çıkardı ve mesafe $\le 8\text{ m}$ (`HearNoise`) | **Araştır** (Mor) |
| **Kovala** (Kırmızı) | Oyuncu $1\text{ sn}$'den uzun süredir görünmüyor | **Ara** (Sarı) |
| **Kovala** (Kırmızı) | Oyuncu ses çıkardı (`HearNoise`) | **Kovala** *(Görmezden gelir)* |
| **Ara** (Sarı) | Kapı kapalı ve kapıya $2{,}5\text{ m}$'den yakın | **KapıdaBekle** (Gri) |
| **Ara** (Sarı) | Oyuncu `N` tuşuyla ses çıkardı ve mesafe $\le 8\text{ m}$ (`HearNoise`) | **Araştır** (Mor) |
| **Ara** (Sarı) | Son görülen noktaya vardı ve $5\text{ sn}$ bakındı | **Devriye** (Yeşil) |
| **Araştır** (Mor) | Kapı kapalı ve kapıya $2{,}5\text{ m}$'den yakın | **KapıdaBekle** (Gri) |
| **Araştır** (Mor) | Sesin geldiği noktaya vardı ve $3\text{ sn}$ bakındı | **Devriye** (Yeşil) |
| **KapıdaBekle** (Gri) | Kapı açıldı (`door.IsOpen == true`) | **Önceki Durum** (`Devriye`, `Ara` ya da `Araştır`) |

### Hafta 7 Ödev Teslimi — Kısa Cevap (Bölüm 10.5)
> **Soru:** `Araştır`'ı ayrı bir durum yapmak yerine `Ara` durumunu kullanabilir miydiniz? Bir avantajı ve bir dezavantajı yazın.
> - **Avantajı:** Yeni bir durum sınıfı (`GuardInvestigate`) yazmaya gerek kalmazdı; `lastSeenPosition` yerine sesin konumu verilip doğrudan `Ara` durumuna geçilerek kod ve sınıf sayısı azaltılırdı.
> - **Dezavantajı:** `Ara` durumu oyuncuyu gözden kaybettikten sonra **sarı** renkle **5 saniye** bakınırken, `Araştır` durumu sesi duyduğunda **farklı bir renkle (mor)** ve **3 saniye** bakınmaktadır; tek durum kullanılsaydı hem görsel geri bildirim/log ayrımı (`Ara` vs `Araştır`) kaybolurdu hem de süre ve davranış farklarını yönetmek için durum içine tekrar `bool isNoise` gibi bayraklar eklemek zorunda kalınarak FSM'nin temel felsefesi bozulurdu.
