# 椤圭洰杩涘害
鐗堟湰: v0.5.3

宸插畬鎴?
- 20260326: 寤虹珛鐙珛鍑嗗績宸ュ叿銆侀厤缃枃浠躲€佸惎鍔?鍋滄鑴氭湰鍜?memory-bank銆?
- 20260327: 淇鏁村睆閫忔槑绐楀鑷寸殑榛戝睆涓庨棯鐑侊紝鏀逛负灏忓昂瀵?overlay锛屼粎鍦ㄧ洰鏍囨父鎴忓墠鍙版樉绀恒€?
- 20260328: 瀹屾垚浜や簰寮忚缃潰鏉裤€丆PU 瑙掓爣銆佺湡瀹炴俯搴︿紭鍏堥摼璺€佷粨搴撳垵濮嬪寲涓庤繙绔彂甯冦€?
- 20260328: 瀹屾垚 AspenBurner 妗岄潰鍖栭噸鏋勶紝浜や粯 WinForms 涓荤▼搴忋€佹墭鐩樸€佽缃獥銆佸吋瀹硅剼鏈拰 CLI銆?
- 20260328: 瀹屾垚鍏煎鍏ュ彛鐑慨锛岃鐩栧弬鏁板吋瀹广€佸崟瀹炰緥閰嶇疆閲嶈浇鍜屽懡浠ら摼鍥炲綊娴嬭瘯銆?
- 20260328: 瀹屾垚 Control Center 鐪熸俯搴︾儹淇紝澧炲姞杩愯搴撳畾浣嶅拰鏈湴缂撳瓨瑁呰浇銆?
- 20260328: 瀹屾垚绋冲畾鎬т笌璁剧疆浣撻獙鐑慨锛屾柊澧炲叏灞€寮傚父鏃ュ織銆乼ick 闃插穿銆佹帹鑽愰璁俱€丷eset銆佽缃嵆鏃跺簲鐢ㄣ€?
- 20260328: 楠岃瘉閫氳繃 `dotnet test` 54/54銆乣dotnet build -c Release` 鎴愬姛銆乣start -> preview -> stop` smoke 鎴愬姛銆?

寰呰瀵?
- 20260328: 鑻ョ敤鎴风户缁姤鍛婃父鎴忓唴涓嶆樉绀烘垨淇濆瓨鏃犳晥锛屼紭鍏堟帓鏌ユ槸鍚﹀瓨鍦ㄦ棫绠＄悊鍛樺疄渚嬫埅娴佸崟瀹炰緥鍛戒护銆?

- 20260328: BIOS/EC investigation complete: Colorful HX public page has no BIOS, Clevo mirror B10723 and EC10708 downloaded, B10724 entry present but unavailable.

- 20260329: Verified BIOS single-flash and EC update succeeded (BIOS 1.07.23, EC 7.08), but firmware throttling persists with fresh Event 37 and CPU capped at 2100 MHz.

- 20260329: 完成 AspenBurner.Bench CPU 验证工具，新增 bench 项目、测试项目、两段负载场景、遥测采样、Event 37 探针和结论分类。
- 20260329: Bench 全量测试通过，并完成本机 75s 正式验证，结果未出现新的 Event 37，当前机器在该工具下表现正常。


- 20260329: Added comparison datapoint for Performance + custom fan + dGPU only; 75s bench remained normal with AvgFreq=3931MHz, PeakTemp=98C, Event37Delta=0.

- 20260329: Added external-fan comparison datapoint; same profile stayed normal, with better FrameLoop P95 and multi-core throughput despite same 98C peak.

- 20260329: Added real-game monitoring result for Delta Force under Performance + Maximum; stable clocks and no Event37, but CPU remained near 98C for most combat samples and FPS capture via PresentMon still failed.

- 20260329: Added AverageTemperatureC to AspenBurner.Bench telemetry/report path and passed targeted bench tests for sampler/formatter/application.
- 20260329: Created and validated local reusable skill clevo-gaming-thermal-tuning under C:\Users\Aspen\.codex\skills\; scripts now switch CC40 via verified AutomationIds, switch Windows power plans, and run AspenBurner.Bench.