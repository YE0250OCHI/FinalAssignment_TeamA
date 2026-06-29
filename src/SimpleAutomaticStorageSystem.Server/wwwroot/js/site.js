console.log("site.js loaded");

const indexUrl = '/picking-orders';
const historyUrl = '/picking-orders/history';

const updateUrl = '/api/v1/picking-orders';
const interval = 3000;

// 出庫依頼更新
const updatePickingOrder = async () => {

    // 要素取得
    const itemSelector = document.getElementById('item-selector');
    const defaultOption = document.getElementById('default-option');
    const orderButton = document.getElementById('order-button');
    const incompleteJobContainer = document.getElementById('incomplete-job-container');

    if (itemSelector === null ||
        defaultOption === null ||
        orderButton === null ||
        incompleteJobContainer === null) {
        console.error('要素が不足している');
        return;
    }

    // 更新フラグ
    const isSelectingItem = document.activeElement === itemSelector;

    // ポーリング処理
    try {
        console.log('実行開始');

        //responseに受け取ったデータを格納
        const response = await fetch(updateUrl);

        if (!response.ok) {
            // ok以外のとき
            throw new Error("Error");
        }

        // レスポンス読取
        const jsonData = await response.json();

        const availables = jsonData.available;
        const orders = jsonData.statuses;

        // 1.出庫可能リスト組立
        if (!isSelectingItem) {
            const isDisable = availables.length === 0;

            if (isDisable) {
                defaultOption.textContent = '-- 出荷可能商品なし --';
            } else {
                defaultOption.textContent = '-- 商品を選択してください --';
            }

            itemSelector.replaceChildren(defaultOption);

            for (const item of availables) {
                const option = document.createElement('option');

                option.value = item.keyCode;
                option.textContent = item.text;

                itemSelector.appendChild(option);
            }

            itemSelector.disabled = isDisable;
            orderButton.disabled = isDisable;
        }

        // 2.未完了出庫依頼組立
        incompleteJobContainer.replaceChildren(
            ...orders.map(createIncompleteJob)
        );

    } catch (e) {
        // エラー表示
        console.error(e);
    }
};

// 未完了出庫依頼カード組立
const createIncompleteJob = (job) => {

    // データ取得
    const jobId = job.jobId;
    const itemCode = job.itemCode;
    const itemName = job.itemName;
    const status = job.status;
    const equipmentId = job.equipmentId;
    const canCancel = job.canCancel;

    // カード本体
    const card = document.createElement('div');
    card.className = 'card shadow-sm mb-2';

    const cardBody = document.createElement('div');
    cardBody.className = 'card-body d-flex align-items-center';

    // 左側：状態・品種名・JOB番号
    const leftArea = document.createElement('div');
    leftArea.className = 'd-flex align-items-center';

    // バッジ用の固定領域
    const badgeArea = document.createElement('div');
    badgeArea.className =
        'd-flex justify-content-center align-items-center flex-shrink-0';

    badgeArea.style.width = '5.5rem';

    // バッジ
    const statusBadge = document.createElement('span');

    const statusInfo = getStatusInfo(status);
    statusBadge.className =
        `badge ${statusInfo.className} p-2 text-center`;

    statusBadge.style.width = '4.5rem';
    statusBadge.textContent = statusInfo.text;

    badgeArea.appendChild(statusBadge);

    // JOB情報
    const itemArea = document.createElement('div');

    // 品種名
    const itemNameArea = document.createElement('div');
    itemNameArea.className = 'fw-bold text-start';
    itemNameArea.append(itemName);

    // (品種コード)
    const itemCodeArea = document.createElement('span');
    itemCodeArea.className = 'text-body-tertiary small ms-1';
    itemCodeArea.textContent = `(${itemCode})`;

    itemNameArea.appendChild(itemCodeArea);

    // JOB番号
    const jobIdArea = document.createElement('div');
    jobIdArea.className = 'text-body-tertiary small mt-1';
    jobIdArea.textContent = jobId;

    itemArea.append(itemNameArea, jobIdArea);
    leftArea.append(badgeArea, itemArea);

    // 右側：装置ID・キャンセルボタン
    const rightArea = document.createElement('div');
    rightArea.className = 'ms-auto d-flex align-items-center gap-3';


    // 装置ID
    // if (equipmentId !== null) {
    //     const equipmentIdArea = document.createElement('div');
    //     equipmentIdArea.className = 'text-body-tertiary small';
    //     equipmentIdArea.textContent = equipmentId;

    //     rightArea.appendChild(equipmentIdArea);
    // }

    // キャンセル用フォーム
    const cancelForm = document.createElement('form');
    cancelForm.method = 'post';
    cancelForm.action = '?handler=Cancel';

    // CSRF対策トークン
    const token = document.querySelector(
        'input[name="__RequestVerificationToken"]'
    );

    if (token !== null) {
        const antiForgeryToken = document.createElement('input');
        antiForgeryToken.type = 'hidden';
        antiForgeryToken.name = '__RequestVerificationToken';
        antiForgeryToken.value = token.value;

        cancelForm.appendChild(antiForgeryToken);
    }

    // 送信用JOB番号
    const jobIdInput = document.createElement('input');
    jobIdInput.type = 'hidden';
    jobIdInput.name = 'jobId';
    jobIdInput.value = jobId;

    // キャンセルボタン
    const cancelButton = document.createElement('button');
    cancelButton.type = 'submit';
    cancelButton.className = 'btn btn-outline-danger btn-sm';
    cancelButton.textContent = 'キャンセル';
    cancelButton.disabled = !canCancel;

    // 後でキャンセル対象を判定する用
    cancelButton.dataset.jobId = jobId;

    cancelForm.append(jobIdInput, cancelButton);
    rightArea.appendChild(cancelForm);

    // カード組立
    cardBody.append(leftArea, rightArea);
    card.appendChild(cardBody);

    return card;
};

// ステータスの変換
const getStatusInfo = (status) => {
    switch (status) {
        case 'waiting':
            return { text: '待機中', className: 'text-bg-secondary' };

        case 'transferring':
            return { text: '搬送中', className: 'text-bg-warning' };

        case 'waitOut':
            return { text: '取出待ち', className: 'text-bg-info' };

        case 'completed':
            return { text: '完了', className: 'text-bg-success' };

        case 'canceled':
            return { text: 'キャンセル', className: 'text-bg-warning' };

        case 'aborted':
            return { text: '異常終了', className: 'text-bg-danger' };

        default:
            throw new Error(`未定義のJOB状態です: ${status}`);
    }
};

// 履歴時のSubmit前検証
const validateBeforeSubmit = () => {

    // 要素取得
    const filterForm = document.getElementById('history-filter-form');
    const fromDate = document.getElementById('from-date');
    const toDate = document.getElementById('to-date');
    const dateError = document.getElementById('date-error');

    if (filterForm === null ||
        fromDate === null ||
        toDate === null ||
        dateError === null) {
        console.error('履歴検索用の要素が不足している');
        return;
    }

    // 両方が入力されているときに、toがfromよりも小さいときにsubmit中止
    filterForm.addEventListener('submit', event => {
        const from = fromDate.value;
        const to = toDate.value;

        if (from !== '' && to !== '' && to < from) {
            event.preventDefault(); // submitを中止

            toDate.classList.add('is-invalid');
            dateError.classList.remove('d-none');
            dateError.textContent = '終了日は開始日以降を指定してください。';
        }
    });


    // 入力再開でエラー解除する関数
    const clearDateErrorIfValid = () => {
        const from = fromDate.value;
        const to = toDate.value;

        if (from === '' || to === '' || to >= from) {
            toDate.classList.remove('is-invalid');
            dateError.classList.add('d-none');
            dateError.textContent = '';
        }
    };

    // エラー解除
    toDate.addEventListener('input', clearDateErrorIfValid);
    fromDate.addEventListener('input', clearDateErrorIfValid);
};

// ローダー
document.addEventListener('DOMContentLoaded', () => {

    // 自身がIndexでなければスキップ
    const currentPath = window.location.pathname.replace(/\/$/, "");

    switch (currentPath) {
        case indexUrl:
            // 出庫依頼更新
            updatePickingOrder();

            // 以降、interval[msec]ごとに実行
            setInterval(updatePickingOrder, interval);

            break;
        case historyUrl:
            // Submit時に検証を入れる
            validateBeforeSubmit();

            break;
        default:
            break;
    };

});
