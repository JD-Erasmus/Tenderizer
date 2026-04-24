(function () {
    function postForm(form) {
        return fetch(form.action, {
            method: (form.method || 'post').toUpperCase(),
            body: new FormData(form),
            credentials: 'same-origin'
        });
    }

    function getAntiForgeryToken(element) {
        const form = element.closest('form');
        const tokenInput = form ? form.querySelector('input[name="__RequestVerificationToken"]') : null;
        return tokenInput instanceof HTMLInputElement ? tokenInput.value : '';
    }

    function createTokenField(token) {
        return token ? `<input type="hidden" name="__RequestVerificationToken" value="${token}">` : '';
    }

    async function handleAjaxFormSubmit(event) {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || !form.matches('[data-checklist-ajax-form="true"]')) {
            return;
        }

        event.preventDefault();

        const response = await postForm(form);
        if (response.ok) {
            window.location.reload();
            return;
        }

        const message = await response.text();
        if (message) {
            window.alert(message);
        }
    }

    async function confirmDelete(form) {
        if (!form) {
            return;
        }

        if (!window.Swal) {
            form.submit();
            return;
        }

        const result = await window.Swal.fire({
            icon: 'warning',
            title: 'Delete this item?',
            text: 'This action cannot be undone.',
            showCancelButton: true,
            confirmButtonText: 'Delete',
            cancelButtonText: 'Cancel',
            confirmButtonColor: '#dc3545'
        });

        if (result.isConfirmed) {
            form.submit();
        }
    }

    async function handleDeleteClick(event) {
        const btn = event.target.closest('[data-delete-button]');
        if (!btn) {
            return;
        }

        event.preventDefault();
        await confirmDelete(btn.closest('form'));
    }

    async function lockSelectedChecklistItem(select) {
        const currentLockItem = select.dataset.checklistCurrentLockItem || '';
        const selectedItem = select.value || '';

        if (!selectedItem) {
            if (currentLockItem) {
                const unlockForm = document.createElement('form');
                unlockForm.method = 'post';
                unlockForm.action = select.dataset.checklistUnlockUrlTemplate.replace('__ID__', currentLockItem);
                unlockForm.innerHTML = createTokenField(getAntiForgeryToken(select));
                const response = await postForm(unlockForm);
                if (!response.ok) {
                    throw new Error(await response.text());
                }
            }

            select.dataset.checklistCurrentLockItem = '';
            updateChecklistState('Select an item to acquire its lock before uploading.');
            return;
        }

        if (currentLockItem === selectedItem) {
            return;
        }

        if (currentLockItem) {
            const unlockForm = document.createElement('form');
            unlockForm.method = 'post';
            unlockForm.action = select.dataset.checklistUnlockUrlTemplate.replace('__ID__', currentLockItem);
            unlockForm.innerHTML = createTokenField(getAntiForgeryToken(select));
            const unlockResponse = await postForm(unlockForm);
            if (!unlockResponse.ok) {
                throw new Error(await unlockResponse.text());
            }
        }

        const lockForm = document.createElement('form');
        lockForm.method = 'post';
        lockForm.action = select.dataset.checklistLockUrlTemplate.replace('__ID__', selectedItem);
        lockForm.innerHTML = createTokenField(getAntiForgeryToken(select));

        const response = await postForm(lockForm);
        if (!response.ok) {
            throw new Error(await response.text());
        }

        select.dataset.checklistCurrentLockItem = selectedItem;
        updateChecklistState('Checklist item locked for upload.');
    }

    function updateChecklistState(text) {
        const state = document.getElementById('checklist-lock-state');
        if (state) {
            state.textContent = text;
        }
    }

    async function handleChecklistSelectChange(event) {
        const select = event.target;
        if (!(select instanceof HTMLSelectElement) || !select.matches('[data-checklist-lock-select="true"]')) {
            return;
        }

        try {
            await lockSelectedChecklistItem(select);
        } catch (error) {
            window.alert(error instanceof Error ? error.message : 'Unable to update checklist lock.');
            select.value = select.dataset.checklistCurrentLockItem || '';
        }
    }

    function init() {
        document.addEventListener('submit', handleAjaxFormSubmit);
        document.addEventListener('click', handleDeleteClick);
        document.addEventListener('change', handleChecklistSelectChange);

        const selects = Array.from(document.querySelectorAll('[data-checklist-lock-select="true"]'));
        for (const select of selects) {
            if (select instanceof HTMLSelectElement && select.value && !select.dataset.checklistCurrentLockItem) {
                lockSelectedChecklistItem(select).catch((error) => {
                    window.alert(error instanceof Error ? error.message : 'Unable to update checklist lock.');
                });
            }
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
