// 頁內籤切換共用邏輯（例如：列表 / 操作紀錄）。
// 用 .admin-view-tab / .admin-view-pane / data-admin-view 這三個掛勾，
// 頁面上有幾組籤就會自動處理幾組，不用每個頁面各寫一份。
document.querySelectorAll('.admin-view-tab').forEach(function (tab) {
    tab.addEventListener('click', function () {
        document.querySelectorAll('.admin-view-tab').forEach(t => t.classList.remove('active'));
        document.querySelectorAll('.admin-view-pane').forEach(p => p.classList.remove('active'));
        tab.classList.add('active');
        document.getElementById(tab.dataset.adminView).classList.add('active');
    });
});
