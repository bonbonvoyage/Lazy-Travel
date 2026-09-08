-- 如果不確定原本執行 SQLQuery3.sql 的那個視窗還在不在、或找不回去了，
-- 先開一個「新的」查詢視窗執行這段，確認是不是真的有一筆未提交的交易卡住。

-- 1) 有沒有還沒結束的交易（有結果代表確實有一筆卡住的交易）
SELECT
    s.session_id,
    s.login_name,
    s.host_name,
    s.program_name,
    r.status,
    r.command,
    t.name AS transaction_name,
    at.transaction_begin_time,
    DATEDIFF(SECOND, at.transaction_begin_time, GETDATE()) AS 已卡住幾秒
FROM sys.dm_tran_active_transactions at
JOIN sys.dm_tran_session_transactions st ON at.transaction_id = st.transaction_id
JOIN sys.dm_exec_sessions s ON st.session_id = s.session_id
LEFT JOIN sys.dm_exec_requests r ON s.session_id = r.session_id
LEFT JOIN sys.transactions t ON t.transaction_id = at.transaction_id;

-- 2) 看看有誰被鎖卡住、被誰卡住（如果後台程式正在重試，這裡會看到 blocking_session_id
--    指向上面那個 session_id）
EXEC sp_who2;

/* ------------------------------------------------------------------------
   看到上面 1) 有結果、而且 program_name 是 SSMS/Microsoft SQL Server Management
   Studio 之類的，就代表是那份種子資料腳本留下的未提交交易，處理方式二選一：

   A. 找得到、能回到原本那個 SSMS 視窗（同一條連線）→ 執行 COMMIT TRAN;
      （就是 commit_pending_transaction.sql 那個檔案），資料保留、鎖立刻放開。

   B. 真的回不去那個視窗了 → 在「這個」新視窗對那個 session_id 執行：
        KILL <session_id>;
      這樣會把那筆未提交的交易整個回滾（ROLLBACK），種子資料等於白新增，
      鎖也會放開。之後要重新塞資料的話，改執行下面附的
      seed_admin_fixed.sql（已經補上 COMMIT TRAN，不會再卡住）。
   ------------------------------------------------------------------------ */
