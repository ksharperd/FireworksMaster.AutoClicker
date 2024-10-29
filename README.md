# C# 版《明日方舟》狂歡煙火大師賽的自動點擊互動工具
## 使用方式
### 從 Source Code 建置：
1. Clone 本 Repo。
2. 在 [這裏](https://github.com/dotnet/sdk/blob/main/documentation/package-table.md) 下載並安裝 **64位元的** dotnet-sdk-10.0.x。
3. 在本 Repo 根目錄執行 `dotnet build -r win-x64 -c Release` 或 `dotnet publish -r win-x64 -c Release`。
4. 執行建置成功後得到的 `FireworksMasterAutoClicker.exe`。

### 預先建置的 Binary：
1. 在 [Release](https://github.com/ksharperd/FireworksMaster.AutoClicker/releases) 下載 **7z** 檔並解壓。
2. 執行解壓得到的 `FireworksMasterAutoClicker.exe`。

## 注意
1. 本 Repo 僅支援 MuMu 模擬器。
2. 推薦的模擬器畫面 ratio 爲 16:9(鎖定橫向) 或 9:16(直向)。
3. 在執行 `FireworksMasterAutoClicker.exe` 前必須啓動 MuMu 模擬器 並打開 森空島 內的活動頁面。
4. 如果執行後沒有在 'FMAC' 視窗內看到模擬器的畫面或發現定位點有偏離，請根據你的情況自行調整 settings.json 內的 MultiEmulatorInstanceIndex、MultiAppInstanceIndex 等關鍵設定項。
5. MultiEmulatorInstanceIndex 是 MuMu 多開器 內左側的 ID，MultiAppInstanceIndex 是應用程式多開的 ID。

## 聲明
1. 明日方舟、森空島等等是鷹角網絡的商標。
2. 本 Repo 僅供娛樂，本人不負任何使用本 Repo 造成的後果及責任。

## 感謝
本 Repo 的部分演算法來自 [xTaiwanPingLord](https://github.com/xTaiwanPingLord) 的 [fireworks_master_autoclicker](https://github.com/xTaiwanPingLord/fireworks_master_autoclicker)