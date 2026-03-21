# 首次 Tag 发版操作说明

1. 在本地仓库确认 `main`/`master`/`release` 分支最新并完成所有待合并的 PR。
2. 使用 `git tag -a v1.0.0 -m "V1.0 发布说明"` 创建遵循 `v*` 前缀的 tag，标签名称可按语义版本（示例 `v1.0.0`）。
3. 推送 tag 到远端：
   ```powershell
   git push origin v1.0.0
   ```
   推送后 GitHub Actions 的 `release.yml` 会自动触发。
4. 若首次执行失败，进入 GitHub 的 `Actions > release` 页面，展开最近一次运行，点击 `Re-run jobs`（优选“Re-run failed jobs”）重新执行当前 tag 的 workflow。
5. 完成后在 Release 页面会看到：
   - `SnakeGame-<tag>-win-x64.zip`
   - `SnakeGame-<tag>-win-x64.sha256`
   - 同步生成的 release note 内容，在 Asset 列表或 workflow 输出中均可以查看。
6. 若需撤销或修正，删除本地及远端 tag（`git tag -d v1.0.0` + `git push origin :refs/tags/v1.0.0`），在主分支上完成修复再创建新的 tag。
