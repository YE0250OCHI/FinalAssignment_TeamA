console.log("site.js loaded");

document.addEventListener('DOMContentLoaded', () => {
    updateTaskList();
});


const updLink = '/api/v1/picking-orders';
const interval = 5000;

let running = true;

//未完了タスクリスト取得
const updatePickingOrder = async () => {

    while (running) {

        try {

            //responseに受け取ったデータを格納
            const response = await fetch(updLink);

            if (!response.ok) {
                // ok以外のとき
                throw new Error("Error");
            }



        } catch (e) {
            // エラー表示
            console.error(e);
        }

        // ポーリング待機
        await new Promise(r =>
            setTimeout(r, interval));
    }
}

async function updateTaskList() {
    try {
        //responseに受け取ったデータを格納
        const response = await fetch('/api/v1/picking-orders');//あとからリンク変更

        //レスポンスがない場合の処理
        if (!response.ok) {
            throw new Error('Network Error');
        }

        //受け取ったデータ全体(データ数の情報含む)はdataに、内容はtasksに代入
        const data = await response.json();
        const tasks = data.results;

        //incompleteTaskListContainerを含むdivをhtmlから探し、containerに代入
        const container = document.getElementById('incompleteTaskListContainer'); 
        //incompleteTaskListContainerが見つからなかったとき
        if (!container) {
            console.error('incompleteTaskListContainer が見つかりません');
            return;
        }

        //htmlの更新内容を入れる変数htmlを定義
        let html = '';

        ```javascript
        container.innerHTML = html;

        // キャンセルボタン押下時はfetchで送信する
        container.querySelectorAll('form').forEach(form => {

            form.addEventListener('submit', async function (event) {

                event.preventDefault();

                try {

                    const response = await fetch(form.action, {
                        method: 'POST',
                        body: new FormData(form)
                    });

                    // 正常終了時は画面更新
                    if (response.ok || response.redirected) {

                        window.location.href = '/picking-orders';
                        return;
                    }

                    // エラーメッセージ取得
                    const errorMessage = await response.text();

                    // エラー表示領域取得
                    let errorArea =
                        document.getElementById('errorMessage');

                    // エラー表示領域が存在しなければ生成
                    if (!errorArea) {

                        errorArea = document.createElement('div');
                        errorArea.id = 'errorMessage';
                        errorArea.className =
                            'text-danger small mb-2';
                        errorArea.style.whiteSpace = 'pre-line';

                        const title =
                            document.querySelector(
                                '.card h5');

                        title.insertAdjacentElement(
                            'afterend',
                            errorArea);
                    }

                    errorArea.textContent = errorMessage;
                }
                catch (error) {

                    console.error(error);

                    window.location.href = '/Error';
                }
            });
        });
```



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

            const token =
                document.querySelector(
                    'input[name="__RequestVerificationToken"]'
                ).value;

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

            '<form method="post" action="/picking-orders/' + encodeURIComponent(item.jobId) + '?handler=Cancel">' +
            '<input type="hidden" name="__RequestVerificationToken" value="' + token + '">'+
                cancelButton +
                '</form>' +

                '</div>';

            console.log(item.jobId);

        });

        container.innerHTML = html;
    }
    catch (error) {
        console.error(error);
        window.location.href = '/Error';
    }
}

// async function loadHistory() {
//     try {
//         const response = await fetch('');あとからリンク変更

//         if (!response.ok) {
//             throw new Error('履歴取得失敗');
//         }

//         const data = await response.json();

//         const historyList = document.getElementById('finishedTskList');

//         if (!historyList) {
//             console.error('finishedTskList が見つかりません');
//             return;
//         }

//         historyList.innerHTML = '';

//         if (data.count === 0) {
//             historyList.innerHTML =
//                 '<div class="text-center text-secondary py-3 small">' +
//                 '対象の履歴はありません。' +
//                 '</div>';

//             return;
//         }

//         data.results.forEach(function (item) {

//             let bgClass = 'bg-danger';
//             let textClass = 'text-danger';

//             switch (item.status) {
//                 case 'キャンセル':
//                     bgClass = 'bg-secondary';
//                     textClass = 'text-secondary';
//                     break;

//                 case '完了':
//                     bgClass = 'bg-warning';
//                     textClass = 'text-warning';
//                     break;
//             }

//             const completedDate =
//                 new Date(item.completed_at)
//                     .toLocaleString('ja-JP');

//             historyList.innerHTML +=
//                 '<div class="d-flex justify-content-between align-items-center ' +
//                 bgClass +
//                 ' bg-opacity-10 border ' +
//                 bgClass +
//                 ' border-opacity-50 rounded-3 p-2 ps-3">' +

//                 '<div class="d-flex align-items-center flex-grow-1">' +

//                 '<span class="' + textClass + ' fw-bold me-3" style="min-width:70px;">' +
//                 item.status +
//                 '</span>' +

//                 '<span class="fw-bold me-2">' +
//                 item.itemName +
//                 '</span>' +

//                 '</div>' +

//                 '<div class="text-secondary small text-end text-nowrap">' +
//                 '完了日：' + completedDate +
//                 '</div>' +

//                 '</div>';
//         });
//     }
//     catch (error) {
//         console.error(error);
//         window.location.href = '/Error';
//     }
// }


// document.addEventListener('DOMContentLoaded', function () {
//     updateTaskList();
//     loadHistory();
// });