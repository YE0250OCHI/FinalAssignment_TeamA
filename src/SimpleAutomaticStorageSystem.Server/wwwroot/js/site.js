async function updateTaskList() {
    try {
        const response = await fetch('?handler=FetchTasks');//あとからリンク変更

        if (!response.ok) {
            throw new Error('Network Error');
        }

        const data = await response.json();
        const tasks = data.results;

        const container = document.getElementById('incompleteTaskListContainer'); 

        if (!container) {
            console.error('incompleteTaskListContainer が見つかりません');
            return;
        }

        let html = '';

        tasks.forEach(function (item) {

            let bgClass = 'bg-secondary';
            let textClass = 'text-secondary';
            let statusText = item.status;

            switch (item.status) {
                case 'WaitOut':
                    bgClass = 'bg-success';
                    textClass = 'text-success';
                    statusText = '取出待ち';
                    break;

                case 'Working':
                    bgClass = 'bg-warning';
                    textClass = 'text-warning';
                    statusText = '作業中';
                    break;

                case 'Recovering':
                    bgClass = 'bg-primary';
                    textClass = 'text-primary';
                    statusText = '調整中';
                    break;

                case 'Waiting':
                    bgClass = 'bg-secondary';
                    textClass = 'text-secondary';
                    statusText = '待機中';
                    break;
            }

            let cancelButton = '';

            if (item.canCancel) {
                cancelButton =
                    '<button type="submit" class="btn btn-danger btn-sm fw-bold">' +
                    'キャンセル' +
                    '</button>';
            }
            else {
                cancelButton =
                    '<button type="button" class="btn btn-secondary btn-sm opacity-50" disabled>' +
                    'キャンセル' +
                    '</button>';
            }

            html +=
                '<div class="d-flex justify-content-between align-items-center ' +
                bgClass +
                ' bg-opacity-10 border ' +
                bgClass +
                ' border-opacity-50 rounded-3 p-2 ps-3">' +

                '<div>' +
                '<span class="' + textClass + ' fw-bold me-3">' +
                statusText +
                '</span>' +

                '<span class="fw-bold">' +
                item.itemName +
                '</span>' +
                '</div>' +

                '<form method="post" action="?handler=Cancel&jobId=' + item.jobId + '">' +
                cancelButton +
                '</form>' +

                '</div>';
        });

        container.innerHTML = html;
    }
    catch (error) {
        console.error(error);
        window.location.href = '/Error';
    }
}


async function loadHistory() {
    try {
        const response = await fetch('?handler=HistoryList');//あとからリンク変更

        if (!response.ok) {
            throw new Error('履歴取得失敗');
        }

        const data = await response.json();

        const historyList = document.getElementById('finishedTskList');

        if (!historyList) {
            console.error('finishedTskList が見つかりません');
            return;
        }

        historyList.innerHTML = '';

        if (data.count === 0) {
            historyList.innerHTML =
                '<div class="text-center text-secondary py-3 small">' +
                '対象の履歴はありません。' +
                '</div>';

            return;
        }

        data.results.forEach(function (item) {

            let bgClass = 'bg-danger';
            let textClass = 'text-danger';

            switch (item.status) {
                case 'キャンセル':
                    bgClass = 'bg-secondary';
                    textClass = 'text-secondary';
                    break;

                case '完了':
                    bgClass = 'bg-warning';
                    textClass = 'text-warning';
                    break;
            }

            const completedDate =
                new Date(item.completed_at)
                    .toLocaleString('ja-JP');

            historyList.innerHTML +=
                '<div class="d-flex justify-content-between align-items-center ' +
                bgClass +
                ' bg-opacity-10 border ' +
                bgClass +
                ' border-opacity-50 rounded-3 p-2 ps-3">' +

                '<div class="d-flex align-items-center flex-grow-1">' +

                '<span class="' + textClass + ' fw-bold me-3" style="min-width:70px;">' +
                item.status +
                '</span>' +

                '<span class="fw-bold me-2">' +
                item.itemName +
                '</span>' +

                '</div>' +

                '<div class="text-secondary small text-end text-nowrap">' +
                '完了日：' + completedDate +
                '</div>' +

                '</div>';
        });
    }
    catch (error) {
        console.error(error);
        window.location.href = '/Error';
    }
}


document.addEventListener('DOMContentLoaded', function () {
    updateTaskList();
    loadHistory();
});