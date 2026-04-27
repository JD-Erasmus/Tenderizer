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

    function init() {
        document.addEventListener('submit', handleAjaxFormSubmit);
        document.addEventListener('click', handleDeleteClick);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
