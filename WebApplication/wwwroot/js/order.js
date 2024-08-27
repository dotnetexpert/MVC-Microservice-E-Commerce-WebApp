var dataTable; function loadDataTable(a) {
    dataTable = $("#tblData").DataTable({
        order: [[0, "desc"]], ajax: { url: "/order/getall?status=" + a }, columns: [{ data: "orderHeaderId", width: "5%" }, { data: "email", width: "25%" }, { data: "name", width: "20%" }, { data: "phone", width: "10%" }, { data: "status", width: "10%" }, { data: "orderTotal", width: "10%" }, {
            data: "orderHeaderId", render: function (a) {
                return `<div class="w-75 btn-group" role="group">
                    <a href="/order/orderDetail?orderId=${a}" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                    </div>`}, width: "10%"
        }], scrollY: "400px", scrollCollapse: !0, paging: !0
    })
} $(document).ready(function () { var a = window.location.search; a.includes("approved") ? loadDataTable("approved") : a.includes("readyforpickup") ? loadDataTable("readyforpickup") : a.includes("cancelled") ? loadDataTable("cancelled") : loadDataTable("all") });