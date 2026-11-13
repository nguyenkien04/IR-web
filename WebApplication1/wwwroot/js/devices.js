function saveDevice() {
    var name = document.getElementById('newDeviceName').value;
    var desc = document.getElementById('newDeviceDesc').value;

    if (!name) {
        alert("Vui lòng nhập tên thiết bị!");
        return;
    }

    fetch('/Home/AddNewDevice', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ DeviceName: name, Description: desc })
    })
    .then(function (res) { return res.json(); })
    .then(function (data) {
        showToast(data.message, data.success);
        if (data.success) {
            location.reload();
        }
    });
}

function deleteDevice(id, name) {
    if (confirm("Bạn có chắc chắn muốn xóa thiết bị " + name + " và toàn bộ nút bấm?")) {
        fetch('/Home/DeleteDevice?id=' + id, { method: 'POST' })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                showToast(data.message, data.success);
                if (data.success) {
                    location.reload();
                }
            });
    }
}

function openRemoteModal(deviceId, deviceName) {
    document.getElementById('remoteModalTitle').innerText = deviceName;
    var area = document.getElementById('remoteButtonsArea');
    area.innerHTML = '<div class="spinner-border text-info my-4" role="status"></div>';

    var modal = new bootstrap.Modal(document.getElementById('remoteModal'));
    modal.show();

    fetch('/Home/GetButtons?deviceId=' + deviceId)
        .then(function (res) { return res.json(); })
        .then(function (data) {
            area.innerHTML = '';
            if (data.length === 0) {
                area.innerHTML = '<p class="text-warning my-4">Thiết bị này chưa có nút bấm nào trong CSDL.</p>';
                return;
            }

            data.forEach(function (btn) {
                var col = document.createElement('div');
                col.className = 'col-6';
                var btnColor = btn.buttonName.toLowerCase().indexOf('nguồn') !== -1 ? 'btn-danger fw-bold' : 'btn-outline-light';

                var btnHtml = '<button class="btn ' + btnColor + ' w-100 py-3 rounded-4 shadow-sm" onclick="sendIRCommand(' + btn.buttonId + ', \'' + btn.protocol + '\', \'' + btn.hexCode + '\', ' + btn.bits + ')"><i class="bi bi-power me-1"></i> ' + btn.buttonName + '</button>';
                col.innerHTML = btnHtml;
                area.appendChild(col);
            });
        });
}

function sendIRCommand(buttonId, protocol, hexCode, bits) {
    var led = document.getElementById('irLed');
    led.className = 'badge bg-warning text-dark rounded-pill px-3 py-2';
    led.innerHTML = '<i class="bi bi-broadcast"></i> SENDING...';

    fetch('/Home/SendIRCommand', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ButtonId: buttonId, Protocol: protocol, HexCode: hexCode, Bits: bits })
    })
    .then(function (res) { return res.json(); })
    .then(function (data) {
        setTimeout(function () {
            led.className = 'badge bg-danger rounded-pill px-3 py-2';
            led.innerHTML = '<i class="bi bi-broadcast"></i> IR READY';
        }, 500);
        showToast(data.message, data.success);
    });
}

function showToast(msg, isSuccess) {
    var toastElem = document.getElementById('statusToast');
    toastElem.className = isSuccess
        ? 'toast align-items-center text-white bg-success border-0'
        : 'toast align-items-center text-white bg-danger border-0';
    document.getElementById('toastMessage').innerText = msg;
    var toast = new bootstrap.Toast(toastElem);
    toast.show();
}