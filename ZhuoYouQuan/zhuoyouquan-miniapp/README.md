# 桌游圈原生微信小程序

这是不依赖 uni-app、Vue、Vite 或 npm 构建的原生微信小程序版本。

## 运行

1. 启动后端服务，确认 `http://localhost:8080/api/game-types` 可访问。
2. 打开微信开发者工具，选择“导入项目”。
3. 项目目录选择 `zhuoyouquan-miniapp-native`。
4. 在“详情 -> 本地设置”中确认已启用“不校验合法域名”。
5. 点击编译。

本地后端的 `mock-login` 为 `true` 时，客户端使用本地开发 code，不调用 `wx.login`。要切换真实微信登录，请在 `utils/config.js` 中将 `mockLogin` 改为 `false`，并将 `baseUrl` 改为已配置的 HTTPS 服务域名。

## 页面

- 附近活动与城市、游戏类型筛选
- 热门活动发现
- 发起活动
- 活动详情与报名
- 消息入口
- 个人资料与我发起的活动
