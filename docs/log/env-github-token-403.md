# 环境限制 · GitHub App token 403

> 非 bug，记录备查。

- **现象**：GitHub 连接器发的 App 令牌 `ghu_` 对建仓/改名/写文件/push 全部返回 403 "Resource not accessible by integration"；环境安全策略也拦截带 token 的 git push。
- **修正**：用户手动建空公开仓库 + 提供 fine-grained PAT，改用 REST Git Data API（blob/tree/commit/ref）以 base64 上传；临时凭据用完即删。
- **遗留**：仓库名仍是 `terrariaMod-`（App token 无改名权限，未能改成中文）；本地 git 未建立 `origin/main` 跟踪引用（纯 cosmetic，不影响远端内容）。
