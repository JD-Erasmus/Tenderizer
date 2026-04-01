(function () {
    const tableRegistry = new Map();

    function parseBoolean(value, fallback) {
        if (typeof value === 'undefined') {
            return fallback;
        }

        return value === 'true';
    }

    function parseNumber(value, fallback) {
        const parsed = Number.parseInt(value, 10);
        return Number.isNaN(parsed) ? fallback : parsed;
    }

    function escapeRegex(value) {
        return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    function buildColumns(table) {
        const headers = Array.from(table.querySelectorAll('thead th'));

        return headers.map((header, index) => ({
            index,
            name: header.dataset.dtName || `column-${index}`,
            orderable: header.dataset.dtOrder !== 'disable',
            searchable: header.dataset.dtSearchable !== 'false',
            visible: header.dataset.dtVisible !== 'false'
        }));
    }

    function createTable(table) {
        if (typeof window.DataTable === 'undefined' || tableRegistry.has(table.id)) {
            return;
        }

        const columns = buildColumns(table);
        const columnIndexByName = Object.fromEntries(columns.map((column) => [column.name, column.index]));
        const orderColumn = table.dataset.datatableOrderColumn || columns[0].name;
        const orderIndex = columnIndexByName[orderColumn] ?? 0;
        const dataTable = new window.DataTable(table, {
            responsive: true,
            paging: parseBoolean(table.dataset.datatablePaging, true),
            searching: parseBoolean(table.dataset.datatableSearching, true),
            info: parseBoolean(table.dataset.datatableInfo, true),
            lengthChange: parseBoolean(table.dataset.datatableLengthChange, true),
            pageLength: parseNumber(table.dataset.datatablePageLength, 10),
            order: [[orderIndex, table.dataset.datatableOrderDir || 'asc']],
            language: {
                searchPlaceholder: table.dataset.datatableSearchPlaceholder || 'Search'
            },
            columns: columns.map((column) => ({
                name: column.name,
                orderable: column.orderable,
                searchable: column.searchable,
                visible: column.visible
            }))
        });

        tableRegistry.set(table.id, {
            dataTable,
            columnIndexByName
        });
    }

    function applyFilters(tableId) {
        const tableState = tableRegistry.get(tableId);
        if (!tableState) {
            return;
        }

        const controls = Array.from(document.querySelectorAll(`[data-datatable-target="${tableId}"]`));
        tableState.dataTable.columns().search('');

        for (const control of controls) {
            const columnName = control.dataset.datatableColumn;
            const columnIndex = tableState.columnIndexByName[columnName];
            if (typeof columnIndex === 'undefined') {
                continue;
            }

            let value = '';
            if (control instanceof HTMLInputElement && control.type === 'checkbox') {
                value = control.checked ? (control.dataset.datatableValue || '') : '';
            } else {
                value = control.value || '';
            }

            if (!value) {
                continue;
            }

            if (control.dataset.datatableExact === 'true') {
                tableState.dataTable.column(columnIndex).search(`^${escapeRegex(value)}$`, true, false);
            } else {
                tableState.dataTable.column(columnIndex).search(value);
            }
        }

        tableState.dataTable.draw();
    }

    function clearFilters(tableId) {
        const tableState = tableRegistry.get(tableId);
        if (!tableState) {
            return;
        }

        const controls = Array.from(document.querySelectorAll(`[data-datatable-target="${tableId}"]`));
        for (const control of controls) {
            if (control instanceof HTMLInputElement && control.type === 'checkbox') {
                control.checked = false;
            } else {
                control.value = '';
            }
        }

        tableState.dataTable.search('');
        tableState.dataTable.columns().search('');
        tableState.dataTable.draw();

        const searchInput = tableState.dataTable.table().container().querySelector('input[type="search"]');
        if (searchInput) {
            searchInput.value = '';
        }
    }

    function wireFilters() {
        const controls = Array.from(document.querySelectorAll('[data-datatable-target]'));
        for (const control of controls) {
            const eventName = control instanceof HTMLInputElement && control.type === 'checkbox'
                ? 'change'
                : 'input';

            control.addEventListener(eventName, function () {
                applyFilters(control.dataset.datatableTarget);
            });

            if (eventName !== 'change') {
                control.addEventListener('change', function () {
                    applyFilters(control.dataset.datatableTarget);
                });
            }
        }

        const clearButtons = Array.from(document.querySelectorAll('[data-datatable-clear]'));
        for (const button of clearButtons) {
            button.addEventListener('click', function () {
                clearFilters(button.dataset.datatableClear);
            });
        }
    }

    function init() {
        const tables = Array.from(document.querySelectorAll('.js-data-table'));
        for (const table of tables) {
            createTable(table);
        }

        wireFilters();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
