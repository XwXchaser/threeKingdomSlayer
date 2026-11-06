---
id: kd_ec884836-2b2c-4da1-9da8-1789b5721597
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# Locus 远程访问（本机配置）

2026-11-06 实测建立。目标是让手机在**非局域网**（流量 / 外出）也能连上这台开发机的 Locus。

## 现状

| 项 | 值 |
|---|---|
| Tailscale | 1.104.5，机器名 `locus-pc`，tailnet `tailcd398c.ts.net`，IPv4 `100.118.189.68` |
| MagicDNS 域名 | `locus-pc.tailcd398c.ts.net` |
| Locus remoteAccess | enabled，port 47831，loopback=true，`interfaces=["Tailscale","以太网"]`，requireConfirmation=true，tokenIdleDays=30 |
| 监听 | `100.118.189.68:47831`（HTTPS/Tailscale）、`192.168.1.214:47831`（HTTPS/局域网）、`127.0.0.1:47831`（**明文 HTTP**，供隧道转发） |
| 证书 | CN=Locus Remote Access，SHA256 `47BB1338A18E7AB442B714840DB69F43304480C031DF57E7FA3A5F978A50FA25`（自签，手机靠配对二维码固定指纹） |

`remoteAccess.interfaces` 的元素是**网卡友好名称**（如 `Tailscale`、`以太网`），不是 GUID。

## 坑

- **winget 装 Tailscale 失败（0x80070643 / MSI 1603）**：根因是本机 `iphlpsvc`（IP Helper）服务被禁用，MSI 直接报 “Some Windows system services required by Tailscale are currently disabled”。修法：`sc config iphlpsvc start= demand` + `sc start iphlpsvc`，再装即可。
- **winget 源**：默认源里 msstore 证书报错，需加 `--source winget`。
- **本机 Windows 防火墙三个 profile 全部关闭**，因此无需为 47831 添加放行规则。
- `tailscale up` 会一直等待登录，脚本里要加 `--timeout`，登录 URL 才会打印出来；Tailscale 登录后网卡才拿到 100.x 地址（登出态是 169.254.x）。

## 接入方式（外网）

- **A 直连（当前用的）**：手机装 Tailscale 同一账号 → 手机连 `https://100.118.189.68:47831`。自签证书，指纹靠二维码固定。
- **B tailscale serve**：`tailscale serve --bg 47831`，再在 Locus「HTTPS 主机名」登记 `locus-pc.tailcd398c.ts.net`。有效证书、可用 6 位配对码，适合手机对自签证书敏感时。
- **C 任意隧道**（frp / UU远程 / cloudflared / ngrok）：指向 `http://127.0.0.1:47831`，再把公网域名登记进「HTTPS 主机名」。**不要**把 47831 直接做路由器端口映射暴露公网。

## 手机端

- Locus Remote 仅 Android：APK 在 `https://github.com/r1n7aro/Locus/releases/latest`（v0.9.3 `locus-remote_0.9.3_android-arm64.apk`）；配对窗口开启后 10 分钟内也可从本机 `/app/locus-remote.apk` 直接下载。
- 配对必须在 PC 侧点「允许」；令牌 30 天空闲失效。
