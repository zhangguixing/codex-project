# 桌游圈后端 MVP

基于 Spring Boot 3、MyBatis-Plus、MySQL 8、Redis、JWT 和 springdoc-openapi 的微信小程序后端。当前实现第一阶段 MVP：微信登录、用户资料、桌游类型、活动发布/筛选/详情、报名/取消报名、我的活动、收藏及基础信用档案。

## 环境与启动

1. 安装 JDK 17+、Maven 3.9+、MySQL 8 和 Redis 7+。
2. 在 MySQL 中执行 [init.sql](src/main/resources/db/init.sql)。
3. 修改 [application.yml](src/main/resources/application.yml) 的 MySQL 密码、JWT 密钥和微信小程序 `app-id`/`app-secret`。
4. 本地联调期间保留 `app.wechat.mock-login: true`；生产环境必须改为 `false` 并填入真实微信凭证。
5. 运行 `mvn spring-boot:run`，服务默认监听 `http://localhost:8080`。

Swagger UI：`http://localhost:8080/swagger-ui/index.html`

## 认证约定

除登录、活动浏览和桌游类型外，接口均要求请求头：`Authorization: Bearer <token>`。`POST /api/auth/wechat-login` 返回 token；小程序中用 `uni.login()` 得到 code 后发送至此接口。

## 联调示例

```powershell
$login = Invoke-RestMethod -Method Post http://localhost:8080/api/auth/wechat-login -ContentType application/json -Body '{"code":"local-user-001"}'
$headers = @{ Authorization = "Bearer $($login.data.token)" }

Invoke-RestMethod http://localhost:8080/api/game-types

$body = @{
  title = '周五狼人杀欢乐局'; gameTypeId = 1; startTime = '2026-08-08T19:30:00'; maxPeople = 8
  city = '北京'; storeName = '朝阳桌游店'; address = '朝阳区示例路 88 号'; fee = 50; aa = $true
  description = '新手可教学，欢迎准时到场。'; newbieFriendly = $true
} | ConvertTo-Json
Invoke-RestMethod -Method Post http://localhost:8080/api/activities -Headers $headers -ContentType application/json -Body $body
Invoke-RestMethod 'http://localhost:8080/api/activities?city=北京&gameTypeId=1&sort=latest'
```

活动状态：`OPEN`（报名中）、`FULL`（已满）、`ONGOING`、`FINISHED`、`CANCELED`。报名在事务中锁定活动记录，达到 `maxPeople` 后自动改为 `FULL`；取消报名会释放名额。

## 前端契约

所有响应统一为 `{ code, message, data }`，成功 `code` 为 `0`。活动列表可使用 `city`、`gameTypeId`、`timeRange`（`today`/`tomorrow`/`weekend`）、`sort`（`latest`/`popular`/`soon`）、`page`、`size` 查询。
