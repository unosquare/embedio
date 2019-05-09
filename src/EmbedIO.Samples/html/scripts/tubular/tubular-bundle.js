/*jslint node: true */
'use strict';

/*
 * Tubular Template engine module
 * @module TubularTemplateServiceModule
 */
var tubularTemplateServiceModule = {
    enums: {
        dataTypes: ['numeric', 'date', 'boolean', 'string'],
        editorTypes: [
            'tbSimpleEditor', 'tbNumericEditor', 'tbDateTimeEditor', 'tbDateEditor',
            'tbDropdownEditor', 'tbTypeaheadEditor', 'tbHiddenField', 'tbCheckboxField', 'tbTextArea'
        ],
        httpMethods: ['POST', 'PUT', 'GET', 'DELETE'],
        gridModes: ['Read-Only', 'Inline', 'Popup', 'Page'],
        formLayouts: ['Simple', 'Two-columns', 'Three-columns'],
        sortDirections: ['Ascending', 'Descending']
    },

    defaults: {
        gridOptions: {
            Pager: true,
            FreeTextSearch: true,
            PageSizeSelector: true,
            PagerInfo: true,
            ExportCsv: true,
            Mode: 'Read-Only',
            RequireAuthentication: false,
            ServiceName: '',
            RequestMethod: 'GET',
            GridName: 'grid'
        },
        formOptions: {
            CancelButton: true,
            SaveUrl: '',
            SaveMethod: 'POST',
            Layout: 'Simple',
            ModelKey: '',
            RequireAuthentication: false,
            ServiceName: '',
            dataUrl: ''
        },
        fieldsSettings: {
            'tbSimpleEditor': {
                ShowLabel: true,
                Placeholder: true,
                Format: false,
                Help: true,
                Required: true,
                ReadOnly: true,
                EditorType: true
            },
            'tbNumericEditor': {
                ShowLabel: true,
                Placeholder: true,
                Format: true,
                Help: true,
                Required: true,
                ReadOnly: true,
                EditorType: false
            },
            'tbDateTimeEditor': {
                ShowLabel: true,
                Placeholder: false,
                Format: true,
                Help: true,
                Required: true,
                ReadOnly: true,
                EditorType: false
            },
            'tbDateEditor': {
                ShowLabel: true,
                Placeholder: false,
                Format: true,
                Help: true,
                Required: true,
                ReadOnly: true,
                EditorType: false
            },
            'tbDropdownEditor': {
                ShowLabel: true,
                Placeholder: false,
                Format: false,
                Help: true,
                Required: true,
                ReadOnly: true,
                EditorType: false
            },
            'tbTypeaheadEditor': {
                ShowLabel: true,
                Placeholder: true,
                Format: false,
                Help: true,
                Required: true,
                ReadOnly: true,
                EditorType: false
            },
            'tbHiddenField': {
                ShowLabel: false,
                Placeholder: false,
                Format: false,
                Help: false,
                Required: false,
                ReadOnly: false,
                EditorType: false
            },
            'tbCheckboxField': {
                ShowLabel: false,
                Placeholder: false,
                Format: false,
                Help: true,
                Required: false,
                ReadOnly: true,
                EditorType: false
            },
            'tbTextArea': {
                ShowLabel: true,
                Placeholder: true,
                Help: true,
                Required: true,
                ReadOnly: true,
                EditorType: false
            }
        }
    },

    /*
     * Create a columns array using a model.
     * 
     * @param {object} model
     * @returns {array} The Columns
     */
    createColumns: function(model) {
        var jsonModel = (model instanceof Array && model.length > 0) ? model[0] : model;
        var columns = [];

        for (var prop in jsonModel) {
            if (jsonModel.hasOwnProperty(prop)) {
                var value = jsonModel[prop];
                // Ignore functions
                if (prop[0] === '$' || typeof (value) === 'function') {
                    continue;
                }

                // Ignore null value, but maybe evaluate another item if there is anymore
                if (value == null) {
                    continue;
                }

                if (typeof value === 'number' || parseFloat(value).toString() == value) {
                    columns.push({ Name: prop, DataType: 'numeric', Template: '{{row.' + prop + ' | number}}' });
                } else if (toString.call(value) === '[object Date]' || isNaN((new Date(value)).getTime()) === false) {
                    columns.push({ Name: prop, DataType: 'date', Template: '{{row.' + prop + ' | date}}' });
                } else if (value.toLowerCase() === 'true' || value.toLowerCase() === 'false') {
                    columns.push({ Name: prop, DataType: 'boolean', Template: '{{row.' + prop + ' ? "TRUE" : "FALSE" }}' });
                } else {
                    var newColumn = { Name: prop, DataType: 'string', Template: '{{row.' + prop + '}}' };

                    if ((/e(-|)mail/ig).test(newColumn.Name)) {
                        newColumn.Template = '<a href="mailto:' + newColumn.Template + '">' + newColumn.Template + '</a>';
                    }

                    columns.push(newColumn);
                }
            }
        }

        var firstSort = false;

        for (var column in columns) {
            if (columns.hasOwnProperty(column)) {
                var columnObj = columns[column];
                columnObj.Label = columnObj.Name.replace(/([a-z])([A-Z])/g, '$1 $2');
                columnObj.EditorType = this.getEditorTypeByDateType(columnObj.DataType);

                // Grid attributes
                columnObj.Searchable = columnObj.DataType === 'string';
                columnObj.Filter = true;
                columnObj.Visible = true;
                columnObj.Sortable = true;
                columnObj.IsKey = false;
                columnObj.SortOrder = 0;
                columnObj.SortDirection = '';
                // Form attributes
                columnObj.ShowLabel = true;
                columnObj.Placeholder = '';
                columnObj.Format = '';
                columnObj.Help = '';
                columnObj.Required = true;
                columnObj.ReadOnly = false;

                if (!firstSort) {
                    columnObj.IsKey = true;
                    columnObj.SortOrder = 1;
                    columnObj.SortDirection = 'Ascending';
                    firstSort = true;
                }
            }
        }

        return columns;
    },

    /**
     * Generates a array with a template for every column
     * 
     * @param {array} columns
     * @returns {array}
     */
    generateFieldsArray: function(columns) {
        return columns.map(function(el) {
            var editorTag = el.EditorType.replace(/([A-Z])/g, function($1) { return "-" + $1.toLowerCase(); });
            var defaults = tubularTemplateServiceModule.defaults.fieldsSettings[el.EditorType];
            
            return '\r\n\t<' + editorTag + ' name="' + el.Name + '"' +
                (defaults.EditorType ? '\r\n\t\teditor-type="' + el.DataType + '" ' : '') +
                (defaults.ShowLabel ? '\r\n\t\tlabel="' + el.Label + '" show-label="' + el.ShowLabel + '"' : '') +
                (defaults.Placeholder ? '\r\n\t\tplaceholder="' + el.Placeholder + '"' : '') +
                (defaults.Required ? '\r\n\t\trequired="' + el.Required + '"' : '') +
                (defaults.ReadOnly ? '\r\n\t\tread-only="' + el.ReadOnly + '"' : '') +
                (defaults.Format ? '\r\n\t\tformat="' + el.Format + '"' : '') +
                (defaults.Help ? '\r\n\t\thelp="' + el.Help + '"' : '') +
                '>\r\n\t</' + editorTag + '>';
        });
    },

    /**
     * Generates the Form's fields template using a column object
     * 
     * @param {array} columns 
     * @returns {string} 
     */
    generateFields: function(columns) {
        return this.generateFieldsArray(columns).join('');
    },

    generatePopup: function(model, title) {
        var columns = this.createColumns(model);

        return '<tb-form model="Model">' +
            '<div class="modal-header"><h3 class="modal-title">' + (title || 'Edit Row') + '</h3></div>' +
            '<div class="modal-body">' +
            this.generateFields(columns) +
            '</div>' +
            '<div class="modal-footer">' +
            '<button class="btn btn-primary" ng-click="savePopup()" ng-disabled="!Model.$valid()">Save</button>' +
            '<button class="btn btn-danger" ng-click="closePopup()" formnovalidate>Cancel</button>' +
            '</div>' +
            '</tb-form>';
    },

    getEditorTypeByDateType: function(dataType) {
        switch (dataType) {
        case 'date':
            return 'tbDateTimeEditor';
        case 'numeric':
            return 'tbNumericEditor';
        case 'boolean':
            return 'tbCheckboxField';
        default:
            return 'tbSimpleEditor';
        }
    },

    /**
     * Generates a new form using the fields model and options
     * 
     * @param {array} fields 
     * @param {object} options 
     * @returns {string} 
     */
    generateForm: function(fields, options) {
        var layout = options.Layout === 'Simple' ? '' : options.Layout.toLowerCase();
        var fieldsArray = this.generateFieldsArray(fields);
        var fieldsMarkup = '';

        if (layout == '') {
            fieldsMarkup = fieldsArray.join('');
        } else {
            fieldsMarkup = "\r\n\t<div class='row'>" +
                (layout == 'two-columns' ?
                    "\r\n\t<div class='col-md-6'>" +
                    fieldsArray.filter(function(i, e) { return (e % 2) == 0; }).join('') +
                    "\r\n\t</div>\r\n\t<div class='col-md-6'>" +
                    fieldsArray.filter(function(i, e) { return (e % 2) == 1; }).join('') +
                    "</div>" :
                    "\r\n\t<div class='col-md-4'>" +
                    fieldsArray.filter(function(i, e) { return (e % 3) == 0; }).join('') +
                    "\r\n\t</div>\r\n\t<div class='col-md-4'>" +
                    fieldsArray.filter(function(i, e) { return (e % 3) == 1; }).join('') +
                    "\r\n\t</div>\r\n\t<div class='col-md-4'>" +
                    fieldsArray.filter(function(i, e) { return (e % 3) == 2; }).join('') +
                    "\r\n\t</div>") +
                "\r\n\t</div>";
        }

        return '<tb-form server-save-method="' + options.SaveMethod + '" ' +
            'model-key="' + options.ModelKey + '" require-authentication="' + options.RequireAuthentication + '" ' +
            'server-url="' + options.dataUrl + '" server-save-url="' + options.SaveUrl + '"' +
            (options.ServiceName != '' ? ' service-name="' + options.ServiceName + '"' : '') + '>' +
            '\r\n\t' + fieldsMarkup +
            '\r\n\t<div>' +
            '\r\n\t\t<button class="btn btn-primary" ng-click="$parent.save()" ng-disabled="!$parent.model.$valid()">Save</button>' +
            (options.CancelButton ? '\r\n\t\t<button class="btn btn-danger" ng-click="$parent.cancel()" formnovalidate>Cancel</button>' : '') +
            '\r\n\t</div>' +
            '\r\n</tb-form>';
    },

    /**
     * Generates the grid's cells markup
     * @param {array} columns 
     * @param {string} mode 
     * @returns {string} 
     */
    generateCells: function(columns, mode) {
        return columns.map(function(el) {
            var editorTag = el.EditorType.replace(/([A-Z])/g, function($1) { return "-" + $1.toLowerCase(); });

            return '\r\n\t\t<tb-cell-template column-name="' + el.Name + '">' +
                '\r\n\t\t\t' +
                (mode == 'Inline' ?
                    '<' + editorTag + ' is-editing="row.$isEditing" value="row.' + el.Name + '">' +
                    '</' + editorTag + '>' :
                     el.Template) +
                '\r\n\t\t</tb-cell-template>';
        }).join('');
    },

    /**
     * Generates a grid markup using a columns model and grids options
     * @param {array} columns 
     * @param {object} options 
     * @returns {string} 
     */
    generateGrid: function(columns, options) {
        var topToolbar = '';
        var bottomToolbar = '';

        if (options.Pager) {
            topToolbar += '\r\n\t<tb-grid-pager class="col-md-6"></tb-grid-pager>';
            bottomToolbar += '\r\n\t<tb-grid-pager class="col-md-6"></tb-grid-pager>';
        }

        if (options.ExportCsv) {
            topToolbar += '\r\n\t<div class="col-md-3">' +
                '\r\n\t\t<div class="btn-group">' +
                '\r\n\t\t<tb-print-button title="Tubular" class="btn-sm"></tb-print-button>' +
                '\r\n\t\t<tb-export-button filename="tubular.csv" css="btn-sm"></tb-export-button>' +
                '\r\n\t\t</div>' +
                '\r\n\t</div>';
        }

        if (options.FreeTextSearch) {
            topToolbar += '\r\n\t<tb-text-search class="col-md-3" css="input-sm"></tb-text-search>';
        }

        if (options.PageSizeSelector) {
            bottomToolbar += '\r\n\t<tb-page-size-selector class="col-md-3" selectorcss="input-sm"></tb-page-size-selector>';
        }

        if (options.PagerInfo) {
            bottomToolbar += '\r\n\t<tb-grid-pager-info class="col-md-3"></tb-grid-pager-info>';
        }

        // TODO: If it's page mode add button
        // TODO: Add Selectable param, default false
        // TODO: Add Name param
        return '<div class="container">' +
            '\r\n<tb-grid server-url="' + options.dataUrl + '" request-method="' + options.RequestMethod + '" class="row" ' +
            'page-size="10" require-authentication="' + options.RequireAuthentication + '" ' +
            (options.ServiceName != '' ? ' service-name="' + options.ServiceName + '"' : '') +
            (options.Mode != 'Read-Only' ? ' editor-mode="' + options.Mode.toLowerCase() + '"' : '') + '>' +
            (topToolbar === '' ? '' : '\r\n\t<div class="row">' + topToolbar + '\r\n\t</div>') +
            '\r\n\t<div class="row">' +
            '\r\n\t<div class="col-md-12">' +
            '\r\n\t<div class="panel panel-default panel-rounded">' +
            '\r\n\t<tb-grid-table class="table-bordered">' +
            '\r\n\t<tb-column-definitions>' +
            (options.Mode != 'Read-Only' ? '\r\n\t\t<tb-column label="Actions"><tb-column-header>{{label}}</tb-column-header></tb-column>' : '') +
            columns.map(function(el) {
                return '\r\n\t\t<tb-column name="' + el.Name + '" label="' + el.Label + '" column-type="' + el.DataType + '" sortable="' + el.Sortable + '" ' +
                    '\r\n\t\t\tis-key="' + el.IsKey + '" searchable="' + el.Searchable + '" ' +
                    (el.Sortable ? '\r\n\t\t\tsort-direction="' + el.SortDirection + '" sort-order="' + el.SortOrder + '" ' : ' ') +
                    'visible="' + el.Visible + '">' +
                    (el.Filter ? '\r\n\t\t\t<tb-column-filter></tb-column-filter>' : '') +
                    '\r\n\t\t\t<tb-column-header>{{label}}</tb-column-header>' +
                    '\r\n\t\t</tb-column>';
            }).join('') +
            '\r\n\t</tb-column-definitions>' +
            '\r\n\t<tb-row-set>' +
            '\r\n\t<tb-row-template ng-repeat="row in $component.rows" row-model="row">' +
            (options.Mode != 'Read-Only' ? '\r\n\t\t<tb-cell-template>' +
                (options.Mode == 'Inline' ? '\r\n\t\t\t<tb-save-button model="row"></tb-save-button>' : '') +
                '\r\n\t\t\t<tb-edit-button model="row"></tb-edit-button>' +
                '\r\n\t\t</tb-cell-template>' : '') +
            this.generateCells(columns, options.Mode) +
            '\r\n\t</tb-row-template>' +
            '\r\n\t</tb-row-set>' +
            '\r\n\t</tb-grid-table>' +
            '\r\n\t</div>' +
            '\r\n\t</div>' +
            '\r\n\t</div>' +
            (bottomToolbar === '' ? '' : '\r\n\t<div class="row">' + bottomToolbar + '\r\n\t</div>') +
            '\r\n</tb-grid>' +
            '\r\n</div>';
    }
};

try {
    module.exports = tubularTemplateServiceModule;
} catch (e) {
    // Ignore
}
(function(angular) {
    'use strict';

    /**
     * @ngdoc module
     * @name tubular
     * 
     * @description 
     * Tubular module. Entry point to get all the Tubular functionality.
     * 
     * It depends upon  {@link tubular.directives}, {@link tubular.services} and {@link tubular.models}.
     */
    angular.module('tubular', ['tubular.directives', 'tubular.services', 'tubular.models', 'LocalStorageModule'])
        .config([
            'localStorageServiceProvider', function(localStorageServiceProvider) {
                localStorageServiceProvider.setPrefix('tubular');
            }
        ])
        .run([
            'tubularHttp', 'tubularOData', 'tubularLocalData',
            function(tubularHttp, tubularOData, tubularLocalData) {
                // register data services
                tubularHttp.registerService('odata', tubularOData);
                tubularHttp.registerService('local', tubularLocalData);
            }
        ])
        /**
         * @ngdoc filter
         * @name errormessage
         * @kind function
         *
         * @description
         * Use `errormessage` to retrieve the friendly message possible in a HTTP Error object.
         * 
         * @param {object} input Input to filter.
         * @returns {string} Formatted error message.
         */
        .filter('errormessage', function() {
            return function(input) {
                if (angular.isDefined(input) && angular.isDefined(input.data) &&
                    input.data &&
                    angular.isDefined(input.data.ExceptionMessage)) {
                    return input.data.ExceptionMessage;
                }

                return input.statusText || "Connection Error";
            };
        })
        /**
         * @ngdoc filter
         * @name numberorcurrency
         * @kind function
         *
         * @description
         * `numberorcurrency` is a hack to hold `currency` and `number` in a single filter.
         */
        .filter("numberorcurrency", [
            "$filter", function($filter) {
                return function(input, format, symbol, fractionSize) {
                    symbol = symbol || "$";
                    fractionSize = fractionSize || 2;

                    if (format === "C") {
                        return $filter("currency")(input, symbol, fractionSize);
                    }

                    if (format === "I") {
                        return parseInt(input);
                    }

                    // default to decimal
                    return $filter("number")(input, fractionSize);
                };
            }
        ])
        /**
         * @ngdoc filter
         * @name moment
         * @kind function
         *
         * @description
         * `moment` is a filter to call format from moment or, if the input is a Date, call Angular's `date` filter.
         */
        .filter("moment", [
            "$filter", function($filter) {
                return function(input, format) {
                    if (angular.isDefined(input) && typeof (input) === "object") {
                        if (typeof moment == 'function' && input !== null) {
                            return input.format(format);
                        } else {
                            return $filter('date')(input);
                        }
                    }

                    return input;
                };
            }
        ]);
})(window.angular);
(function (angular) {
    'use strict';

    /**
     * @ngdoc module
     * @name tubular.directives
     * @module tubular.directives
     * 
     * @description 
     * Tubular Directives and Components module.
     * 
     * It depends upon {@link tubular.services} and {@link tubular.models}.
     */
    angular.module('tubular.directives', ['tubular.services', 'tubular.models'])
        /**
         * @ngdoc component
         * @name tbGrid
         * 
         * @description
         * The `tbGrid` directive is the base to create any grid. This is the root node where you should start
         * designing your grid. Don't need to add a `controller`.
         * 
         * @param {string} serverUrl Set the HTTP URL where the data comes.
         * @param {string} serverSaveUrl Set the HTTP URL where the data will be saved.
         * @param {string} serverDeleteUrl Set the HTTP URL where the data will be saved.
         * @param {string} serverSaveMethod Set HTTP Method to save data.
         * @param {int} pageSize Define how many records to show in a page, default 20.
         * @param {function} onBeforeGetData Callback to execute before to get data from service.
         * @param {string} requestMethod Set HTTP Method to get data.
         * @param {string} serviceName Define Data service (name) to retrieve data, defaults `tubularHttp`.
         * @param {bool} requireAuthentication Set if authentication check must be executed, default true.
         * @param {string} gridName Grid's name, used to store metainfo in localstorage.
         * @param {string} editorMode Define if grid is read-only or it has editors (inline or popup).
         * @param {bool} showLoading Set if an overlay will show when it's loading data, default true.
         * @param {bool} autoRefresh Set if the grid refresh after any insertion or update, default true.
         * @param {bool} savePage Set if the grid autosave current page, default true.
         * @param {bool} savePageSize Set if the grid autosave page size, default true.
         * @param {bool} saveSearch Set if the grid autosave search, default true.
         */
        .component('tbGrid', {
            template: '<div>' +
                '<div class="tubular-overlay" ng-show="$ctrl.showLoading && $ctrl.currentRequest != null">' +
                '<div><div class="fa fa-refresh fa-2x fa-spin"></div></div></div>' +
                '<ng-transclude></ng-transclude>' +
                '</div>',
            transclude: true,
            bindings: {
                serverUrl: '@',
                serverSaveUrl: '@',
                serverDeleteUrl: '@',
                serverSaveMethod: '@',
                pageSize: '=?',
                onBeforeGetData: '=?',
                requestMethod: '@',
                dataServiceName: '@?serviceName',
                requireAuthentication: '@?',
                name: '@?gridName',
                editorMode: '@?',
                showLoading: '=?',
                autoRefresh: '=?',
                savePage: '=?',
                savePageSize: '=?',
                saveSearch: '=?'
            },
            controller: [
                '$scope', 'localStorageService', 'tubularPopupService', 'tubularModel', 'tubularHttp', '$routeParams',
                function($scope, localStorageService, tubularPopupService, TubularModel, tubularHttp, $routeParams) {
                    var $ctrl = this;

                    $ctrl.$onInit = function() {
                        $ctrl.tubularDirective = 'tubular-grid';

                        $ctrl.name = $ctrl.name || 'tbgrid';
                        $ctrl.columns = [];
                        $ctrl.rows = [];

                        $ctrl.savePage = angular.isUndefined($ctrl.savePage) ? true : $ctrl.savePage;
                        $ctrl.currentPage = $ctrl.savePage ? (localStorageService.get($ctrl.name + "_page") || 1) : 1;

                        $ctrl.savePageSize = angular.isUndefined($ctrl.savePageSize) ? true : $ctrl.savePageSize;
                        $ctrl.pageSize = angular.isUndefined($ctrl.pageSize) ? 20 : $ctrl.pageSize;
                        $ctrl.saveSearch = angular.isUndefined($ctrl.saveSearch) ? true : $ctrl.saveSearch;
                        $ctrl.totalPages = 0;
                        $ctrl.totalRecordCount = 0;
                        $ctrl.filteredRecordCount = 0;
                        $ctrl.requestedPage = $ctrl.currentPage;
                        $ctrl.hasColumnsDefinitions = false;
                        $ctrl.requestCounter = 0;
                        $ctrl.requestMethod = $ctrl.requestMethod || 'POST';
                        $ctrl.serverSaveMethod = $ctrl.serverSaveMethod || 'POST';
                        $ctrl.requestTimeout = 15000;
                        $ctrl.currentRequest = null;
                        $ctrl.autoSearch = $routeParams.param || ($ctrl.saveSearch ? (localStorageService.get($ctrl.name + "_search") || '') : '');
                        $ctrl.search = {
                            Text: $ctrl.autoSearch,
                            Operator: $ctrl.autoSearch == '' ? 'None' : 'Auto'
                        };

                        $ctrl.isEmpty = false;
                        $ctrl.tempRow = new TubularModel($scope, $ctrl, {}, $ctrl.dataService);
                        $ctrl.dataService = tubularHttp.getDataService($ctrl.dataServiceName);
                        $ctrl.requireAuthentication = $ctrl.requireAuthentication || true;
                        tubularHttp.setRequireAuthentication($ctrl.requireAuthentication);
                        $ctrl.editorMode = $ctrl.editorMode || 'none';
                        $ctrl.canSaveState = false;
                        $ctrl.groupBy = '';
                        $ctrl.showLoading = angular.isUndefined($ctrl.showLoading) ? true : $ctrl.showLoading;
                        $ctrl.autoRefresh = angular.isUndefined($ctrl.autoRefresh) ? true : $ctrl.autoRefresh;
                        $ctrl.serverDeleteUrl = $ctrl.serverDeleteUrl || $ctrl.serverSaveUrl;

                        // Emit a welcome message
                        $scope.$emit('tbGrid_OnGreetParentController', $ctrl);
                    };

                    $scope.$watch('$ctrl.columns', function() {
                        if ($ctrl.hasColumnsDefinitions === false || $ctrl.canSaveState === false) {
                            return;
                        }

                        localStorageService.set($ctrl.name + "_columns", $ctrl.columns);
                    }, true);

                    $scope.$watch('$ctrl.serverUrl', function(newVal, prevVal) {
                        if ($ctrl.hasColumnsDefinitions === false || $ctrl.currentRequest || newVal === prevVal) {
                            return;
                        }

                        $ctrl.retrieveData();
                    }, true);

                    $scope.$watch('$ctrl.hasColumnsDefinitions', function(newVal) {
                        if (newVal !== true) return;

                        var isGrouping = false;
                        // Check columns
                        angular.forEach($ctrl.columns, function(column) {
                            if (column.IsGrouping) {
                                if (isGrouping) {
                                    throw 'Only one column is allowed to grouping';
                                }

                                isGrouping = true;
                                column.Visible = false;
                                column.Sortable = true;
                                column.SortOrder = 1;
                                $ctrl.groupBy = column.Name;
                            }
                        });

                        angular.forEach($ctrl.columns, function(column) {
                            if ($ctrl.groupBy == column.Name) return;

                            if (column.Sortable && column.SortOrder > 0) {
                                column.SortOrder++;
                            }
                        });

                        $ctrl.retrieveData();
                    });

                    $scope.$watch('$ctrl.pageSize', function() {
                        if ($ctrl.hasColumnsDefinitions && $ctrl.requestCounter > 0) {
                            if ($ctrl.savePageSize) {
                                localStorageService.set($ctrl.name + "_pageSize", $ctrl.pageSize);
                            }
                            $ctrl.retrieveData();
                        }
                    });

                    $scope.$watch('$ctrl.requestedPage', function() {
                        if ($ctrl.hasColumnsDefinitions && $ctrl.requestCounter > 0) {
                            $ctrl.retrieveData();
                        }
                    });

                    $ctrl.saveSearch = function() {
                        if ($ctrl.saveSearch) {
                            if ($ctrl.search.Text === '') {
                                localStorageService.remove($ctrl.name + "_search");
                            } else {
                                localStorageService.set($ctrl.name + "_search", $ctrl.search.Text);
                            }
                        }
                    };

                    $ctrl.addColumn = function(item) {
                        if (item.Name == null) {
                            return;
                        }

                        if ($ctrl.hasColumnsDefinitions !== false) {
                            throw 'Cannot define more columns. Column definitions have been sealed';
                        }

                        $ctrl.columns.push(item);
                    };

                    $ctrl.newRow = function(template, popup, size, data) {
                        $ctrl.tempRow = new TubularModel($scope, $ctrl, data || {}, $ctrl.dataService);
                        $ctrl.tempRow.$isNew = true;
                        $ctrl.tempRow.$isEditing = true;
                        $ctrl.tempRow.$component = $ctrl;

                        if (angular.isDefined(template) && angular.isDefined(popup) && popup) {
                            tubularPopupService.openDialog(template, $ctrl.tempRow, $ctrl, size);
                        }
                    };

                    $ctrl.deleteRow = function(row) {
                        var urlparts = $ctrl.serverDeleteUrl.split('?');
                        var url = urlparts[0] + "/" + row.$key;

                        if (urlparts.length > 1) {
                            url += '?' + urlparts[1];
                        }

                        var request = {
                            serverUrl: url,
                            requestMethod: 'DELETE',
                            timeout: $ctrl.requestTimeout,
                            requireAuthentication: $ctrl.requireAuthentication
                        };

                        $ctrl.currentRequest = $ctrl.dataService.retrieveDataAsync(request);

                        $ctrl.currentRequest.promise.then(
                            function(data) {
                                row.$hasChanges = false;
                                $scope.$emit('tbGrid_OnRemove', data);
                            }, function(error) {
                                $scope.$emit('tbGrid_OnConnectionError', error);
                            }).then(function() {
                            $ctrl.currentRequest = null;
                            $ctrl.retrieveData();
                        });
                    };

                    $ctrl.verifyColumns = function() {
                        var columns = localStorageService.get($ctrl.name + "_columns");
                        if (columns == null || columns === "") {
                            // Nothing in settings, saving initial state
                            localStorageService.set($ctrl.name + "_columns", $ctrl.columns);
                            return;
                        }

                        for (var index in columns) {
                            if (columns.hasOwnProperty(index)) {
                                var columnName = columns[index].Name;
                                var filtered = $ctrl.columns.filter(function(el) { return el.Name == columnName; });

                                if (filtered.length === 0) {
                                    continue;
                                }

                                var current = filtered[0];
                                // Updates visibility by now
                                current.Visible = columns[index].Visible;

                                // Update sorting
                                if ($ctrl.requestCounter < 1) {
                                    current.SortOrder = columns[index].SortOrder;
                                    current.SortDirection = columns[index].SortDirection;
                                }

                                // Update Filters
                                if (current.Filter != null && current.Filter.Text != null) {
                                    continue;
                                }

                                if (columns[index].Filter != null && columns[index].Filter.Text != null && columns[index].Filter.Operator != 'None') {
                                    current.Filter = columns[index].Filter;
                                }
                            }
                        }
                    };

                    $ctrl.retrieveData = function() {
                        // If the ServerUrl is empty skip data load
                        if ($ctrl.serverUrl == '') {
                            return;
                        }

                        $ctrl.canSaveState = true;
                        $ctrl.verifyColumns();

                        if ($ctrl.savePageSize) {
                            $ctrl.pageSize = (localStorageService.get($ctrl.name + "_pageSize") || $ctrl.pageSize);
                        }

                        if ($ctrl.pageSize < 10) $ctrl.pageSize = 20; // default

                        var skip = ($ctrl.requestedPage - 1) * $ctrl.pageSize;

                        if (skip < 0) skip = 0;

                        var request = {
                            serverUrl: $ctrl.serverUrl,
                            requestMethod: $ctrl.requestMethod || 'POST',
                            timeout: $ctrl.requestTimeout,
                            requireAuthentication: $ctrl.requireAuthentication,
                            data: {
                                Count: $ctrl.requestCounter,
                                Columns: $ctrl.columns,
                                Skip: skip,
                                Take: parseInt($ctrl.pageSize),
                                Search: $ctrl.search,
                                TimezoneOffset: new Date().getTimezoneOffset()
                            }
                        };

                        if ($ctrl.currentRequest !== null) {
                            // This message is annoying when you connect errors to toastr
                            //$ctrl.currentRequest.cancel('tubularGrid(' + $ctrl.$id + '): new request coming.');
                            return;
                        }

                        if (angular.isUndefined($ctrl.onBeforeGetData) === false) {
                            $ctrl.onBeforeGetData();
                        }

                        $scope.$emit('tbGrid_OnBeforeRequest', request, $ctrl);

                        $ctrl.currentRequest = $ctrl.dataService.retrieveDataAsync(request);

                        $ctrl.currentRequest.promise.then(
                            function(data) {
                                $ctrl.requestCounter += 1;

                                if (angular.isUndefined(data) || data == null) {
                                    $scope.$emit('tbGrid_OnConnectionError', {
                                        statusText: "Data is empty",
                                        status: 0
                                    });

                                    return;
                                }

                                $ctrl.dataSource = data;

                                $ctrl.rows = data.Payload.map(function(el) {
                                    var model = new TubularModel($scope, $ctrl, el, $ctrl.dataService);
                                    model.$component = $ctrl;

                                    model.editPopup = function (template, size) {
                                        tubularPopupService.openDialog(template, new TubularModel($scope, $ctrl, el, $ctrl.dataService), $ctrl, size);
                                    };

                                    return model;
                                });

                                $scope.$emit('tbGrid_OnDataLoaded', $ctrl);

                                $ctrl.aggregationFunctions = data.AggregationPayload;
                                $ctrl.currentPage = data.CurrentPage;
                                $ctrl.totalPages = data.TotalPages;
                                $ctrl.totalRecordCount = data.TotalRecordCount;
                                $ctrl.filteredRecordCount = data.FilteredRecordCount;
                                $ctrl.isEmpty = $ctrl.filteredRecordCount === 0;

                                if ($ctrl.savePage) {
                                    localStorageService.set($ctrl.name + "_page", $ctrl.currentPage);
                                }
                            }, function(error) {
                                $ctrl.requestedPage = $ctrl.currentPage;
                                $scope.$emit('tbGrid_OnConnectionError', error);
                            }).then(function() {
                            $ctrl.currentRequest = null;
                        });
                    };

                    $ctrl.sortColumn = function(columnName, multiple) {
                        var filterColumn = $ctrl.columns.filter(function(el) {
                            return el.Name === columnName;
                        });

                        if (filterColumn.length === 0) return;

                        var column = filterColumn[0];

                        if (column.Sortable === false) return;

                        // need to know if it's currently sorted before we reset stuff
                        var currentSortDirection = column.SortDirection;
                        var toBeSortDirection = currentSortDirection === 'None' ? 'Ascending' : currentSortDirection === 'Ascending' ? 'Descending' : 'None';

                        // the latest sorting takes less priority than previous sorts
                        if (toBeSortDirection === 'None') {
                            column.SortOrder = -1;
                            column.SortDirection = 'None';
                        } else {
                            column.SortOrder = Number.MAX_VALUE;
                            column.SortDirection = toBeSortDirection;
                        }

                        // if it's not a multiple sorting, remove the sorting from all other columns
                        if (multiple === false) {
                            angular.forEach($ctrl.columns.filter(function(col) { return col.Name !== columnName; }), function(col) {
                                col.SortOrder = -1;
                                col.SortDirection = 'None';
                            });
                        }

                        // take the columns that actually need to be sorted in order to reindex them
                        var currentlySortedColumns = $ctrl.columns.filter(function(col) {
                            return col.SortOrder > 0;
                        });

                        // reindex the sort order
                        currentlySortedColumns.sort(function(a, b) {
                            return a.SortOrder == b.SortOrder ? 0 : a.SortOrder > b.SortOrder;
                        });

                        currentlySortedColumns.forEach(function(col, index) {
                            col.SortOrder = index + 1;
                        });

                        $scope.$broadcast('tbGrid_OnColumnSorted');

                        $ctrl.retrieveData();
                    };

                    $ctrl.selectedRows = function() {
                        var rows = localStorageService.get($ctrl.name + "_rows");
                        if (rows == null || rows === "") {
                            rows = [];
                        }

                        return rows;
                    };

                    $ctrl.clearSelection = function() {
                        angular.forEach($ctrl.rows, function(value) {
                            value.$selected = false;
                        });

                        localStorageService.set($ctrl.name + "_rows", []);
                    };

                    $ctrl.isEmptySelection = function() {
                        return $ctrl.selectedRows().length === 0;
                    };

                    $ctrl.selectFromSession = function(row) {
                        row.$selected = $ctrl.selectedRows().filter(function(el) {
                            return el.$key === row.$key;
                        }).length > 0;
                    };

                    $ctrl.changeSelection = function(row) {
                        if (angular.isUndefined(row)) return;

                        row.$selected = !row.$selected;

                        var rows = $ctrl.selectedRows();

                        if (row.$selected) {
                            rows.push({ $key: row.$key });
                        } else {
                            rows = rows.filter(function(el) {
                                return el.$key !== row.$key;
                            });
                        }

                        localStorageService.set($ctrl.name + "_rows", rows);
                    };

                    $ctrl.getFullDataSource = function(callback) {
                        $ctrl.dataService.retrieveDataAsync({
                            serverUrl: $ctrl.serverUrl,
                            requestMethod: $ctrl.requestMethod || 'POST',
                            timeout: $ctrl.requestTimeout,
                            requireAuthentication: $ctrl.requireAuthentication,
                            data: {
                                Count: $ctrl.requestCounter,
                                Columns: $ctrl.columns,
                                Skip: 0,
                                Take: -1,
                                Search: {
                                    Text: '',
                                    Operator: 'None'
                                }
                            }
                        }).promise.then(
                            function(data) {
                                callback(data.Payload);
                            }, function(error) {
                                $scope.$emit('tbGrid_OnConnectionError', error);
                            }).then(function() {
                            $ctrl.currentRequest = null;
                        });
                    };

                    $ctrl.visibleColumns = function() {
                        return $ctrl.columns.filter(function(el) { return el.Visible; }).length;
                    };
                }
            ]
        })
        /**
         * @ngdoc directive
         * @name tbGridTable
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbGridTable` directive generate the HTML table where all the columns and rowsets can be defined. 
         * `tbGridTable` requires a parent `tbGrid`.
         * 
         * This directive is replace by a `table` HTML element.
         * 
         * @scope
         */
        .directive('tbGridTable', [
            function() {
                return {
                    require: '^tbGrid',
                    template: '<table ng-transclude class="table tubular-grid-table"></table>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: true,
                    controller: [
                        '$scope', function($scope) {
                            $scope.$component = $scope.$parent.$parent.$ctrl;
                            $scope.tubularDirective = 'tubular-grid-table';
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbColumnDefinitions
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbColumnDefinitions` directive is a parent node to fill with `tbColumn`.
         * 
         * This directive is replace by a `thead` HTML element.
         * 
         * @scope
         */
        .directive('tbColumnDefinitions', [
            function() {

                return {
                    require: '^tbGridTable',
                    template: '<thead><tr ng-transclude></tr></thead>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: true,
                    controller: [
                        '$scope', function($scope) {
                            $scope.$component = $scope.$parent.$parent.$component;
                            $scope.tubularDirective = 'tubular-column-definitions';
                        }
                    ],
                    compile: function compile() {
                        return {
                            post: function(scope) {
                                scope.$component.hasColumnsDefinitions = true;
                            }
                        };
                    }
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbColumn
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbColumn` directive creates a column in the grid's model. 
         * All the attributes are used to generate a `ColumnModel`.
         * 
         * This directive is replace by a `th` HTML element.
         * 
         * @param {string} name Set the column name.
         * @param {string} label Set the column label, if empty column's name is used.
         * @param {boolean} sortable Set if column is sortable.
         * @param {number} sortOrder Set the sorting order, -1 if you don't want to set one.
         * @param {string} sortDirection Set the sorting direction, empty for none and valid values: Ascending and Descending.
         * @param {boolean} isKey Set if column is Model's key.
         * @param {boolean} searchable Set if column is searchable.
         * @param {boolean} visible Set if column is visible.
         * @param {string} columnType Set the column data type. Values: string, numeric, date, datetime, or boolean.
         * @param {boolean} isGrouping Define a group key.
         */
        .directive('tbColumn', [
            function () {
                return {
                    require: '^tbColumnDefinitions',
                    template: '<th ng-transclude ng-class="{sortable: column.Sortable}" ng-show="column.Visible"></th>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: {
                        visible: '=',
                        label: '@',
                        name: '@',
                        sortable: '=?',
                        sortOrder: '=?',
                        isKey: '=?',
                        searchable: '=?',
                        columnType: '@?',
                        isGrouping: '=?',
                        aggregate: '@?',
                        metaAggregate: '@?',
                        sortDirection: '@?'
                    },
                    controller: [
                        '$scope', function ($scope) {
                            $scope.column = { Label: '' };
                            $scope.$component = $scope.$parent.$parent.$component;
                            $scope.tubularDirective = 'tubular-column';
                            $scope.label = angular.isDefined($scope.label) ? $scope.label : ($scope.name || '').replace(/([a-z])([A-Z])/g, '$1 $2');

                            $scope.sortColumn = function (multiple) {
                                $scope.$component.sortColumn($scope.column.Name, multiple);
                            };

                            $scope.$watch("visible", function (val) {
                                if (angular.isDefined(val)) {
                                    $scope.column.Visible = val;
                                }
                            });

                            $scope.$watch('label', function() {
                                $scope.column.Label = $scope.label;
                                // this broadcast here is used for backwards compatibility with tbColumnHeader requiring a scope.label value on its own
                                $scope.$broadcast('tbColumn_LabelChanged', $scope.label);
                            });

                            var column = new function () {
                                this.Name = $scope.name || null;
                                this.Label = $scope.label || null;
                                this.Sortable = $scope.sortable;
                                this.SortOrder = parseInt($scope.sortOrder) || -1;
                                this.SortDirection = function () {
                                    if (angular.isUndefined($scope.sortDirection)) {
                                        return 'None';
                                    }

                                    if ($scope.sortDirection.toLowerCase().indexOf('asc') === 0) {
                                        return 'Ascending';
                                    }

                                    if ($scope.sortDirection.toLowerCase().indexOf('desc') === 0) {
                                        return 'Descending';
                                    }

                                    return 'None';
                                }();

                                this.IsKey = $scope.isKey === "true";
                                this.Searchable = angular.isDefined($scope.searchable) ? $scope.searchable : false;
                                this.Visible = $scope.visible === "false" ? false : true;
                                this.Filter = null;
                                this.DataType = $scope.columnType || "string";
                                this.IsGrouping = $scope.isGrouping === "true";
                                this.Aggregate = $scope.aggregate || "none";
                                this.MetaAggregate = $scope.metaAggregate || "none";
                            };
                            
                            $scope.$component.addColumn(column);
                            $scope.column = column;
                            $scope.label = column.Label;
                        }
                    ]
                };
            }])
        /**
         * @ngdoc directive
         * @module tubular.directives
         * @name tbColumnHeader
         * @restrict E
         *
         * @description
         * The `tbColumnHeader` directive creates a column header, and it must be inside a `tbColumn`. 
         * This directive has functionality to sort the column, the `sortable` attribute is declared in the parent element.
         * 
         * This directive is replace by an `a` HTML element.
         * 
         * @scope
         */
        .directive('tbColumnHeader', [
            function() {
                return {
                    require: '^tbColumn',
                    template: '<span><a title="Click to sort. Press Ctrl to sort by multiple columns" class="column-header" href ng-click="sortColumn($event)">' +
                        '<span class="column-header-default">{{ $parent.column.Label }}</span>' +
                        '<ng-transclude></ng-transclude></a> ' +
                        '<i class="fa sort-icon" ng-class="' + "{'fa-long-arrow-up': $parent.column.SortDirection == 'Ascending', 'fa-long-arrow-down': $parent.column.SortDirection == 'Descending'}" + '">&nbsp;</i>' +
                        '</span>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false,
                    controller: [
                        '$scope', function($scope) {
                            $scope.sortColumn = function($event) {
                                $scope.$parent.sortColumn($event.ctrlKey);
                            };
                            // this listener here is used for backwards compatibility with tbColumnHeader requiring a scope.label value on its own
                            $scope.$on('tbColumn_LabelChanged', function($event, value) {
                                $scope.label = value;
                            });
                        }
                    ],
                    link: function($scope, $element) {
                        if ($element.find('ng-transclude').length > 0) {
                            $element.find('span')[0].remove();
                        }

                        if (!$scope.$parent.column.Sortable) {
                            $element.find('a').replaceWith($element.find('a').children());
                        }
                    }
                }
            }
        ])
        /**
         * @ngdoc directive
         * @name tbRowSet
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbRowSet` directive is used to handle any `tbRowTemplate`. You can define multiples `tbRowSet` for grouping.
         * 
         * This directive is replace by an `tbody` HTML element.
         * 
         * @scope
         */
        .directive('tbRowSet', [
            function() {

                return {
                    require: '^tbGrid',
                    template: '<tbody ng-transclude></tbody>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false,
                    controller: [
                        '$scope', function($scope) {
                            $scope.$component = $scope.$parent.$component || $scope.$parent.$parent.$component;
                            $scope.tubularDirective = 'tubular-row-set';
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbFootSet
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbFootSet` directive is to handle footer.
         * 
         * This directive is replace by an `tfoot` HTML element.
         * 
         * @scope
         */
        .directive('tbFootSet', [
            function() {

                return {
                    require: '^tbGrid',
                    template: '<tfoot ng-transclude></tfoot>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false,
                    controller: [
                        '$scope', function($scope) {
                            $scope.$component = $scope.$parent.$component || $scope.$parent.$parent.$component;
                            $scope.tubularDirective = 'tubular-foot-set';
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbRowTemplate
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbRowTemplate` directive should be use with a `ngRepeat` to iterate all the rows or grouped rows in a rowset.
         * 
         * This directive is replace by an `tr` HTML element.
         * 
         * @scope
         * 
         * @param {object} rowModel Set the current row, if you are using a ngRepeat you must to use the current element variable here.
         * @param {bool} selectable Flag the rowset to allow user to select rows.
         */
        .directive('tbRowTemplate', [
            function() {

                return {
                    template: '<tr ng-transclude' +
                        ' ng-class="{\'info\': selectableBool && model.$selected}"' +
                        ' ng-click="changeSelection(model)"></tr>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: {
                        model: '=rowModel',
                        selectable: '@'
                    },
                    controller: [
                        '$scope', function($scope) {
                            $scope.tubularDirective = 'tubular-rowset';
                            $scope.fields = [];
                            $scope.hasFieldsDefinitions = false;
                            $scope.selectableBool = $scope.selectable == "true";
                            $scope.$component = $scope.$parent.$parent.$parent.$component;

                            $scope.$watch('hasFieldsDefinitions', function(newVal) {
                                if (newVal !== true || angular.isUndefined($scope.model)) {
                                    return;
                                }

                                $scope.bindFields();
                            });

                            $scope.bindFields = function() {
                                angular.forEach($scope.fields, function(field) {
                                    field.bindScope();
                                });
                            };

                            if ($scope.selectableBool && angular.isDefined($scope.model)) {
                                $scope.$component.selectFromSession($scope.model);
                            }

                            $scope.changeSelection = function(rowModel) {
                                if (!$scope.selectableBool) {
                                    return;
                                }

                                $scope.$component.changeSelection(rowModel);
                            };
                        }
                    ],
                    compile: function compile() {
                        return {
                            post: function(scope) {
                                scope.hasFieldsDefinitions = true;
                            }
                        };
                    }
                };
            }
        ])

        /**
         * @ngdoc directive
         * @name tbCellTemplate
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbCellTemplate` directive represents the final table element, a cell, where it can 
         * hold an in-line editor or a plain AngularJS expression related to the current element in the `ngRepeat`.
         * 
         * This directive is replace by an `td` HTML element.
         * 
         * @scope
         * 
         * @param {string} columnName Setting the related column, by passing the name, the cell can share attributes (like visibility) with the column.
         */
        .directive('tbCellTemplate', [
            function () {

                return {
                    require: '^tbRowTemplate',
                    template: '<td ng-transclude ng-show="column.Visible" data-label="{{::column.Label}}"></td>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: {
                        columnName: '@?'
                    },
                    controller: [
                        '$scope', function ($scope) {
                            $scope.column = { Visible: true };
                            $scope.columnName = $scope.columnName || null;
                            $scope.$component = $scope.$parent.$parent.$component;

                            $scope.getFormScope = function () {
                                // TODO: Implement a form in inline editors
                                return null;
                            };

                            if ($scope.columnName != null) {
                                var columnModel = $scope.$component.columns
                                    .filter(function (el) { return el.Name === $scope.columnName; });

                                if (columnModel.length > 0) {
                                    $scope.column = columnModel[0];
                                }
                            }
                        }
                    ]
                };
            }
        ]);
})(window.angular);
(function (angular) {
    'use strict';

    if (typeof moment == 'function') {
        moment.fn.toJSON = function() { return this.format(); }
    }

    var canUseHtml5Date = function() {
        var input = document.createElement('input');
        input.setAttribute('type', 'date');

        var notADateValue = 'not-a-date';
        input.setAttribute('value', notADateValue);

        return (input.value !== notADateValue);
    }();

    angular.module('tubular.directives')
        /**
         * @ngdoc component
         * @name tbSimpleEditor
         * @module tubular.directives
         * 
         * @description
         * The `tbSimpleEditor` component is the basic input to show in a grid or form.
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @param {string} name Set the field name.
         * @param {object} value Set the value.
         * @param {boolean} isEditing Indicate if the field is showing editor.
         * @param {string} editorType Set what HTML input type should display.
         * @param {boolean} showLabel Set if the label should be display.
         * @param {string} label Set the field's label otherwise the name is used.
         * @param {string} placeholder Set the placeholder text.
         * @param {string} help Set the help text.
         * @param {boolean} required Set if the field is required.
         * @param {boolean} readOnly Set if the field is read-only.
         * @param {number} min Set the minimum characters.
         * @param {number} max Set the maximum characters.
         * @param {string} regex Set the regex validation text.
         * @param {string} regexErrorMessage Set the regex validation error message.
         * @param {string} match Set the field name to match values.
         */
        .component('tbSimpleEditor', {
            template: '<div ng-class="{ \'form-group\' : $ctrl.showLabel && $ctrl.isEditing, \'has-error\' : !$ctrl.$valid && $ctrl.$dirty() }">' +
                '<span ng-hide="$ctrl.isEditing">{{$ctrl.value}}</span>' +
                '<label ng-show="$ctrl.showLabel">{{ $ctrl.label }}</label>' +
                '<input type="{{$ctrl.editorType}}" placeholder="{{$ctrl.placeholder}}" ng-show="$ctrl.isEditing" ng-model="$ctrl.value" class="form-control" ' +
                ' ng-required="$ctrl.required" ng-readonly="$ctrl.readOnly" name="{{$ctrl.name}}" />' +
                '<span class="help-block error-block" ng-show="$ctrl.isEditing" ng-repeat="error in $ctrl.state.$errors">{{error}}</span>' +
                '<span class="help-block" ng-show="$ctrl.isEditing && $ctrl.help">{{$ctrl.help}}</span>' +
                '</div>',
            bindings: {
                regex: '@?',
                regexErrorMessage: '@?',
                value: '=?',
                isEditing: '=?',
                editorType: '@',
                showLabel: '=?',
                label: '@?',
                required: '=?',
                min: '=?',
                max: '=?',
                name: '@',
                placeholder: '@?',
                readOnly: '=?',
                help: '@?',
                match: '@?'
            },
            controller: [
                'tubularEditorService', '$scope', '$filter', function (tubularEditorService, $scope, $filter) {
                    var $ctrl = this;
                    
                    $ctrl.validate = function () {
                        if (angular.isDefined($ctrl.regex) && $ctrl.regex != null && angular.isDefined($ctrl.value) && $ctrl.value != null && $ctrl.value != '') {
                            var patt = new RegExp($ctrl.regex);

                            if (patt.test($ctrl.value) === false) {
                                $ctrl.$valid = false;
                                $ctrl.state.$errors = [$ctrl.regexErrorMessage || $filter('translate')('EDITOR_REGEX_DOESNT_MATCH')];
                                return;
                            }
                        }

                        if (angular.isDefined($ctrl.match) && $ctrl.match) {
                            if ($ctrl.value != $scope.$component.model[$ctrl.match]) {
                                var label = $filter('filter')($scope.$component.fields, { name: $ctrl.match }, true)[0].label;
                                $ctrl.$valid = false;
                                $ctrl.state.$errors = [$filter('translate')('EDITOR_MATCH', label)];
                                return;
                            }
                        }

                        if (angular.isDefined($ctrl.min) && angular.isDefined($ctrl.value) && $ctrl.value != null) {
                            if ($ctrl.value.length < parseInt($ctrl.min)) {
                                $ctrl.$valid = false;
                                $ctrl.state.$errors = [$filter('translate')('EDITOR_MIN_CHARS', $ctrl.min)];
                                return;
                            }
                        }

                        if (angular.isDefined($ctrl.max) && angular.isDefined($ctrl.value) && $ctrl.value != null) {
                            if ($ctrl.value.length > parseInt($ctrl.max)) {
                                $ctrl.$valid = false;
                                $ctrl.state.$errors = [$filter('translate')('EDITOR_MAX_CHARS', $ctrl.max)];
                                return;
                            }
                        }
                    };

                    $ctrl.$onInit = function() {
                        tubularEditorService.setupScope($scope, null, $ctrl, false);
                    };
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbNumericEditor
         * @module tubular.directives
         *
         * @description
         * The `tbNumericEditor` component is numeric input, similar to `tbSimpleEditor` 
         * but can render an add-on to the input visual element.
         * 
         * When you need a numeric editor but without the visual elements you can use 
         * `tbSimpleEditor` with the `editorType` attribute with value `number`.
         * 
         * This component uses the `TubularModel` to retrieve the model information.
         * 
         * @param {string} name Set the field name.
         * @param {object} value Set the value.
         * @param {boolean} isEditing Indicate if the field is showing editor.
         * @param {boolean} showLabel Set if the label should be display.
         * @param {string} label Set the field's label otherwise the name is used.
         * @param {string} placeholder Set the placeholder text.
         * @param {string} help Set the help text.
         * @param {boolean} required Set if the field is required.
         * @param {string} format Indicate the format to use, C for currency otherwise number.
         * @param {boolean} readOnly Set if the field is read-only.
         * @param {number} min Set the minimum value.
         * @param {number} max Set the maximum value.
         * @param {number} step Set the step setting, default 'any'.
         */
        .component('tbNumericEditor', {
            template: '<div ng-class="{ \'form-group\' : $ctrl.showLabel && $ctrl.isEditing, \'has-error\' : !$ctrl.$valid && $ctrl.$dirty() }">' +
                '<span ng-hide="$ctrl.isEditing">{{$ctrl.value | numberorcurrency: format }}</span>' +
                '<label ng-show="$ctrl.showLabel">{{ $ctrl.label }}</label>' +
                '<div class="input-group" ng-show="$ctrl.isEditing">' +
                '<div class="input-group-addon" ng-hide="$ctrl.format == \'I\'">' +
                '<i ng-class="{ \'fa\': true, \'fa-calculator\': $ctrl.format != \'C\', \'fa-usd\': $ctrl.format == \'C\'}"></i>' +
                '</div>' +
                '<input type="number" placeholder="{{$ctrl.placeholder}}" ng-model="$ctrl.value" class="form-control" ' +
                'ng-required="$ctrl.required" ng-hide="$ctrl.readOnly" step="{{$ctrl.step || \'any\'}}"  name="{{$ctrl.name}}" />' +
                '<p class="form-control form-control-static text-right" ng-show="$ctrl.readOnly">{{$ctrl.value | numberorcurrency: format}}</span></p>' +
                '</div>' +
                '<span class="help-block error-block" ng-show="$ctrl.isEditing" ng-repeat="error in $ctrl.state.$errors">{{error}}</span>' +
                '<span class="help-block" ng-show="$ctrl.isEditing && $ctrl.help">{{$ctrl.help}}</span>' +
                '</div>',
            bindings: {
                value: '=?',
                isEditing: '=?',
                showLabel: '=?',
                label: '@?',
                required: '=?',
                format: '@?',
                min: '=?',
                max: '=?',
                name: '@',
                placeholder: '@?',
                readOnly: '=?',
                help: '@?',
                step: '=?'
            },
            controller: [
                'tubularEditorService', '$scope', '$filter', function (tubularEditorService, $scope, $filter) {
                    var $ctrl = this;
                    $ctrl.validate = function () {
                        if (angular.isDefined($ctrl.min) && angular.isDefined($ctrl.value) && $ctrl.value != null) {
                            $ctrl.$valid = $ctrl.value >= $ctrl.min;
                            if (!$ctrl.$valid) {
                                $ctrl.state.$errors = [$filter('translate')('EDITOR_MIN_NUMBER', $ctrl.min)];
                            }
                        }

                        if (!$ctrl.$valid) {
                            return;
                        }

                        if (angular.isDefined($ctrl.max) && angular.isDefined($ctrl.value) && $ctrl.value != null) {
                            $ctrl.$valid = $ctrl.value <= $ctrl.max;
                            if (!$ctrl.$valid) {
                                $ctrl.state.$errors = [$filter('translate')('EDITOR_MAX_NUMBER', $ctrl.max)];
                            }
                        }
                    };

                    $ctrl.$onInit = function() {
                        $ctrl.DataType = "numeric";

                        tubularEditorService.setupScope($scope, 0, $ctrl, false);
                    };
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbDateTimeEditor
         * @module tubular.directives
         *
         * @description
         * The `tbDateTimeEditor` component is date/time input. It uses the `datetime-local` HTML5 attribute, but if this
         * components fails it falls back to Angular UI Bootstrap Datepicker (time functionality is unavailable).
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @param {string} name Set the field name.
         * @param {object} value Set the value.
         * @param {boolean} isEditing Indicate if the field is showing editor.
         * @param {boolean} showLabel Set if the label should be display.
         * @param {string} label Set the field's label otherwise the name is used.
         * @param {string} help Set the help text.
         * @param {boolean} required Set if the field is required.
         * @param {string} format Indicate the format to use, default "yyyy-MM-dd HH:mm".
         * @param {boolean} readOnly Set if the field is read-only.
         * @param {number} min Set the minimum value.
         * @param {number} max Set the maximum value.
         */
        .component('tbDateTimeEditor', {
            template: '<div ng-class="{ \'form-group\' : $ctrl.showLabel && $ctrl.isEditing, \'has-error\' : !$ctrl.$valid && $ctrl.$dirty() }">' +
                '<span ng-hide="$ctrl.isEditing">{{ $ctrl.value | date: format }}</span>' +
                '<label ng-show="$ctrl.showLabel">{{ $ctrl.label }}</label>' +
                (canUseHtml5Date ?
                    '<input type="datetime-local" ng-show="$ctrl.isEditing" ng-model="$ctrl.value" class="form-control" ' +
                    'ng-required="$ctrl.required" ng-readonly="$ctrl.readOnly" name="{{$ctrl.name}}"/>' :
                    '<div class="input-group" ng-show="$ctrl.isEditing">' +
                    '<input type="text" uib-datepicker-popup="{{$ctrl.format}}" ng-model="$ctrl.value" class="form-control" ' +
                    'ng-required="$ctrl.required" ng-readonly="$ctrl.readOnly" name="{{$ctrl.name}}" is-open="$ctrl.open" />' +
                    '<span class="input-group-btn">' +
                    '<button type="button" class="btn btn-default" ng-click="$ctrl.open = !$ctrl.open"><i class="fa fa-calendar"></i></button>' +
                    '</span>' +
                    '</div>') +
                '{{error}}' +
                '</span>' +
                '<span class="help-block" ng-show="$ctrl.isEditing && $ctrl.help">{{$ctrl.help}}</span>' +
                '</div>',
            bindings: {
                value: '=?',
                isEditing: '=?',
                showLabel: '=?',
                label: '@?',
                required: '=?',
                format: '@?',
                min: '=?',
                max: '=?',
                name: '@',
                readOnly: '=?',
                help: '@?'
            },
            controller: [
                '$scope', '$element', 'tubularEditorService', '$filter', function ($scope, $element, tubularEditorService, $filter) {
                    var $ctrl = this;

                    // This could be $onChange??
                    $scope.$watch(function () {
                        return $ctrl.value;
                    }, function (val) {
                        if (typeof (val) === 'string') {
                            $ctrl.value = new Date(val);
                        }
                    });

                    $ctrl.validate = function () {
                        if (angular.isDefined($ctrl.min)) {
                            if (Object.prototype.toString.call($ctrl.min) !== "[object Date]") {
                                $ctrl.min = new Date($ctrl.min);
                            }

                            $ctrl.$valid = $ctrl.value >= $ctrl.min;

                            if (!$ctrl.$valid) {
                                $ctrl.state.$errors = [$filter('translate')('EDITOR_MIN_DATE', $filter('date')($ctrl.min, $ctrl.format))];
                            }
                        }

                        if (!$ctrl.$valid) {
                            return;
                        }

                        if (angular.isDefined($ctrl.max)) {
                            if (Object.prototype.toString.call($ctrl.max) !== "[object Date]") {
                                $ctrl.max = new Date($ctrl.max);
                            }

                            $ctrl.$valid = $ctrl.value <= $ctrl.max;

                            if (!$ctrl.$valid) {
                                $ctrl.state.$errors = [$filter('translate')('EDITOR_MAX_DATE', $filter('date')($ctrl.max, $ctrl.format))];
                            }
                        }
                    };

                    $ctrl.$onInit = function () {
                        $ctrl.DataType = "date";
                        tubularEditorService.setupScope($scope, $ctrl.format, $ctrl);
                    };
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbDateEditor
         * @module tubular.directives
         *
         * @description
         * The `tbDateEditor` component is date input. It uses the `datetime-local` HTML5 attribute, but if this
         * components fails it falls back to a Angular UI Bootstrap Datepicker.
         * 
         * Similar to `tbDateTimeEditor` but without a timepicker.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @param {string} name Set the field name.
         * @param {object} value Set the value.
         * @param {boolean} isEditing Indicate if the field is showing editor.
         * @param {boolean} showLabel Set if the label should be display.
         * @param {string} label Set the field's label otherwise the name is used.
         * @param {string} help Set the help text.
         * @param {boolean} required Set if the field is required.
         * @param {string} format Indicate the format to use, default "yyyy-MM-dd".
         * @param {boolean} readOnly Set if the field is read-only.
         * @param {number} min Set the minimum value.
         * @param {number} max Set the maximum value.
         */
        .component('tbDateEditor', {
            template: '<div ng-class="{ \'form-group\' : $ctrl.showLabel && $ctrl.isEditing, \'has-error\' : !$ctrl.$valid && $ctrl.$dirty() }">' +
                '<span ng-hide="$ctrl.isEditing">{{ $ctrl.value | moment: $ctrl.format }}</span>' +
                '<label ng-show="$ctrl.showLabel">{{ $ctrl.label }}</label>' +
                (canUseHtml5Date ?
                    '<input type="date" ng-show="$ctrl.isEditing" ng-model="$ctrl.dateValue" class="form-control" ' +
                    'ng-required="$ctrl.required" ng-readonly="$ctrl.readOnly" name="{{$ctrl.name}}"/>' : 
                    '<div class="input-group" ng-show="$ctrl.isEditing">' +
                    '<input type="text" uib-datepicker-popup="{{$ctrl.format}}" ng-model="$ctrl.dateValue" class="form-control" ' +
                    'ng-required="$ctrl.required" ng-readonly="$ctrl.readOnly" name="{{$ctrl.name}}" is-open="$ctrl.open" />' +
                    '<span class="input-group-btn">' +
                    '<button type="button" class="btn btn-default" ng-click="$ctrl.open = !$ctrl.open"><i class="fa fa-calendar"></i></button>' +
                    '</span>' +
                    '</div>') +
                '<span class="help-block error-block" ng-show="$ctrl.isEditing" ng-repeat="error in $ctrl.state.$errors">' +
                '{{error}}' +
                '</span>' +
                '<span class="help-block" ng-show="$ctrl.isEditing && $ctrl.help">{{$ctrl.help}}</span>' +
                '</div>',
            bindings: {
                value: '=?',
                isEditing: '=?',
                showLabel: '=?',
                label: '@?',
                required: '=?',
                format: '@?',
                min: '=?',
                max: '=?',
                name: '@',
                readOnly: '=?',
                help: '@?'
            },
            controller: [
               '$scope', '$element', 'tubularEditorService', '$filter', function ($scope, $element, tubularEditorService, $filter) {
                   var $ctrl = this;

                   $scope.$watch(function() {
                       return $ctrl.value;
                   }, function (val) {
                       if (angular.isUndefined(val)) return;

                       if (typeof (val) === 'string') {
                           $ctrl.value = typeof moment == 'function' ? moment(val) : new Date(val);
                       }

                       if (angular.isUndefined($ctrl.dateValue)) {
                           if (typeof moment == 'function') {
                               var tmpDate = $ctrl.value.toObject();
                               $ctrl.dateValue = new Date(tmpDate.years, tmpDate.months, tmpDate.date, tmpDate.hours, tmpDate.minutes, tmpDate.seconds);
                           } else {
                               $ctrl.dateValue = $ctrl.value;
                           }

                           $scope.$watch(function() {
                               return $ctrl.dateValue;
                           }, function(val) {
                               if (angular.isDefined(val)) {
                                   $ctrl.value = typeof moment == 'function' ? moment(val) : new Date(val);
                               }
                           });
                       }
                   });

                   $ctrl.validate = function () {
                       if (angular.isDefined($ctrl.min)) {
                           if (Object.prototype.toString.call($ctrl.min) !== "[object Date]") {
                               $ctrl.min = new Date($ctrl.min);
                           }

                           $ctrl.$valid = $ctrl.dateValue >= $ctrl.min;

                           if (!$ctrl.$valid) {
                               $ctrl.state.$errors = [$filter('translate')('EDITOR_MIN_DATE', $filter('date')($ctrl.min, $ctrl.format))];
                           }
                       }

                       if (!$ctrl.$valid) {
                           return;
                       }

                       if (angular.isDefined($ctrl.max)) {
                           if (Object.prototype.toString.call($ctrl.max) !== "[object Date]") {
                               $ctrl.max = new Date($ctrl.max);
                           }

                           $ctrl.$valid = $ctrl.dateValue <= $ctrl.max;

                           if (!$ctrl.$valid) {
                               $ctrl.state.$errors = [$filter('translate')('EDITOR_MAX_DATE', $filter('date')($ctrl.max, $ctrl.format))];
                           }
                       }
                   };

                   $ctrl.$onInit = function() {
                        $ctrl.DataType = "date";
                        tubularEditorService.setupScope($scope, $ctrl.format, $ctrl);

                        if (typeof moment == 'function' && angular.isUndefined($ctrl.format)) {
                           $ctrl.format = "MMM D, Y";
                       }
                   };
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbDropdownEditor
         * @module tubular.directives
         *
         * @description
         * The `tbDropdownEditor` component is drowpdown editor, it can get information from a HTTP 
         * source or it can be an object declared in the attributes.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @param {string} name Set the field name.
         * @param {object} value Set the value.
         * @param {boolean} isEditing Indicate if the field is showing editor.
         * @param {boolean} showLabel Set if the label should be display.
         * @param {string} label Set the field's label otherwise the name is used.
         * @param {string} help Set the help text.
         * @param {boolean} required Set if the field is required.
         * @param {boolean} readOnly Set if the field is read-only.
         * @param {object} options Set the options to display.
         * @param {string} optionsUrl Set the Http Url where to retrieve the values.
         * @param {string} optionsMethod Set the Http Method where to retrieve the values.
         * @param {string} optionLabel Set the property to get the labels.
         * @param {string} optionKey Set the property to get the keys.
         * @param {string} defaultValue Set the default value.
         */
        .component('tbDropdownEditor', {
            template: '<div ng-class="{ \'form-group\' : $ctrl.showLabel && $ctrl.isEditing, \'has-error\' : !$ctrl.$valid && $ctrl.$dirty() }">' +
                '<span ng-hide="$ctrl.isEditing">{{ $ctrl.value }}</span>' +
                '<label ng-show="$ctrl.showLabel">{{ $ctrl.label }}</label>' +
                '<select ng-options="{{ $ctrl.selectOptions }}" ng-show="$ctrl.isEditing" ng-model="$ctrl.value" class="form-control" ' +
                'ng-required="$ctrl.required" ng-disabled="$ctrl.readOnly" name="{{$ctrl.name}}" />' +
                '<span class="help-block error-block" ng-show="$ctrl.isEditing" ng-repeat="error in $ctrl.state.$errors">' +
                '{{error}}' +
                '</span>' +
                '<span class="help-block" ng-show="$ctrl.isEditing && $ctrl.help">{{$ctrl.help}}</span>' +
                '</div>',
            bindings: {
                value: '=?',
                isEditing: '=?',
                showLabel: '=?',
                label: '@?',
                required: '=?',
                name: '@',
                readOnly: '=?',
                help: '@?',
                defaultValue: '@?',
                options: '=?',
                optionsUrl: '@',
                optionsMethod: '@?',
                optionLabel: '@?',
                optionKey: '@?',
                optionTrack: '@?'
            },
            controller: [
                'tubularEditorService', '$scope', function(tubularEditorService, $scope) {
                    var $ctrl = this;

                    $ctrl.$onInit = function() {
                        tubularEditorService.setupScope($scope, null, $ctrl);
                        $ctrl.dataIsLoaded = false;
                        $ctrl.selectOptions = "d for d in $ctrl.options";

                        if (angular.isDefined($ctrl.optionLabel)) {
                            $ctrl.selectOptions = "d." + $ctrl.optionLabel + " for d in options";

                            if (angular.isDefined($ctrl.optionTrack)) {
                                $ctrl.selectOptions = 'd as d.' + $ctrl.optionLabel + ' for d in options track by d.' + $ctrl.optionTrack;
                            }
                            else {
                                if (angular.isDefined($ctrl.optionKey)) {
                                    $ctrl.selectOptions = 'd.' + $ctrl.optionKey + ' as ' + $ctrl.selectOptions;
                                }
                            }
                        }

                        if (angular.isDefined($ctrl.optionsUrl)) {
                            $scope.$watch('optionsUrl', function(val, prev) {
                                if (val === prev) return;

                                $ctrl.dataIsLoaded = false;
                                $ctrl.loadData();
                            });

                            if ($ctrl.isEditing) {
                                $ctrl.loadData();
                            } else {
                                $scope.$watch('$ctrl.isEditing', function () {
                                    if ($ctrl.isEditing) {
                                        $ctrl.loadData();
                                    }
                                });
                            }
                        }
                    };

                    $scope.$watch(function() {
                        return $ctrl.value;
                    }, function(val) {
                        $scope.$emit('tbForm_OnFieldChange', $ctrl.$component, $ctrl.name, val, $scope.options);
                    });

                    $ctrl.loadData = function() {
                        if ($ctrl.dataIsLoaded) {
                            return;
                        }

                        if (angular.isUndefined($ctrl.$component) || $ctrl.$component == null) {
                            throw 'You need to define a parent Form or Grid';
                        }

                        var currentRequest = $ctrl.$component.dataService.retrieveDataAsync({
                            serverUrl: $ctrl.optionsUrl,
                            requestMethod: $ctrl.optionsMethod || 'GET'
                        });

                        var value = $ctrl.value;
                        $ctrl.value = '';

                        currentRequest.promise.then(
                            function(data) {
                                $ctrl.options = data;
                                $ctrl.dataIsLoaded = true;
                                // TODO: Add an attribute to define if autoselect is OK
                                var possibleValue = $ctrl.options && $ctrl.options.length > 0 ?
                                    angular.isDefined($ctrl.optionKey) ? $ctrl.options[0][$ctrl.optionKey] : $ctrl.options[0]
                                    : '';
                                $ctrl.value = value || $ctrl.defaultValue || possibleValue;

                                // Set the field dirty
                                var formScope = $ctrl.getFormField();
                                if (formScope) formScope.$setDirty();
                            }, function(error) {
                                $scope.$emit('tbGrid_OnConnectionError', error);
                            });
                    };
                }
            ]
        })
        /**
         * @ngdoc directive
         * @name tbTypeaheadEditor
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbTypeaheadEditor` directive is autocomplete editor, it can get information from a HTTP source or it can get them
         * from a object declared in the attributes.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @scope
         * 
         * @param {string} name Set the field name.
         * @param {object} value Set the value.
         * @param {boolean} isEditing Indicate if the field is showing editor.
         * @param {boolean} showLabel Set if the label should be display.
         * @param {string} label Set the field's label otherwise the name is used.
         * @param {string} help Set the help text.
         * @param {boolean} required Set if the field is required.
         * @param {object} options Set the options to display.
         * @param {string} optionsUrl Set the Http Url where to retrieve the values.
         * @param {string} optionsMethod Set the Http Method where to retrieve the values.
         * @param {string} optionLabel Set the property to get the labels.
         * @param {string} css Set the CSS classes for the input.
         */
        .directive('tbTypeaheadEditor', [
            'tubularEditorService', '$q', '$compile', function (tubularEditorService, $q, $compile) {

                return {
                    restrict: 'E',
                    replace: true,
                    scope: {
                        value: '=?',
                        isEditing: '=?',
                        showLabel: '=?',
                        label: '@?',
                        required: '=?',
                        name: '@',
                        placeholder: '@?',
                        readOnly: '=?',
                        help: '@?',
                        options: '=?',
                        optionsUrl: '@',
                        optionsMethod: '@?',
                        optionLabel: '@?',
                        css: '@?'
                    },
                    link: function (scope, element) {
                        var template = '<div ng-class="{ \'form-group\' : showLabel && isEditing, \'has-error\' : !$valid && $dirty() }">' +
                            '<span ng-hide="isEditing">{{ value }}</span>' +
                            '<label ng-show="showLabel">{{ label }}</label>' +
                            '<div class="input-group" ng-show="isEditing">' +
                            '<input ng-model="value" placeholder="{{placeholder}}" title="{{tooltip}}" autocomplete="off" ' +
                            'class="form-control {{css}}" ng-readonly="readOnly || lastSet.indexOf(value) !== -1" uib-typeahead="' + scope.selectOptions + '" ' +
                            'ng-required="required" name="{{name}}" /> ' +
                            '<div class="input-group-addon" ng-hide="lastSet.indexOf(value) !== -1"><i class="fa fa-pencil"></i></div>' +
                            '<span class="input-group-btn" ng-show="lastSet.indexOf(value) !== -1" tabindex="-1">' +
                            '<button class="btn btn-default" type="button" ng-click="value = null"><i class="fa fa-times"></i>' +
                            '</span>' +
                            '</div>' +
                            '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">' +
                            '{{error}}' +
                            '</span>' +
                            '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                            '</div>';

                        var linkFn = $compile(template);
                        var content = linkFn(scope);
                        element.append(content);
                    },
                    controller: [
                        '$scope', function ($scope) {
                            tubularEditorService.setupScope($scope);
                            $scope.selectOptions = "d for d in getValues($viewValue)";
                            $scope.lastSet = [];

                            if (angular.isDefined($scope.optionLabel)) {
                                $scope.selectOptions = "d as d." + $scope.optionLabel + " for d in getValues($viewValue)";
                            }

                            $scope.$watch('value', function (val) {
                                $scope.$emit('tbForm_OnFieldChange', $scope.$component, $scope.name, val, $scope.options);
                                $scope.tooltip = val;
                                if (angular.isDefined(val) && val != null && angular.isDefined($scope.optionLabel)) {
                                    $scope.tooltip = val[$scope.optionLabel];
                                }
                            });

                            $scope.getValues = function (val) {
                                if (angular.isDefined($scope.optionsUrl)) {
                                    if (angular.isUndefined($scope.$component) || $scope.$component == null) {
                                        throw 'You need to define a parent Form or Grid';
                                    }

                                    var p = $scope.$component.dataService.retrieveDataAsync({
                                        serverUrl: $scope.optionsUrl + '?search=' + val,
                                        requestMethod: $scope.optionsMethod || 'GET'
                                    }).promise;

                                    p.then(function (data) {
                                        $scope.lastSet = data;
                                        return data;
                                    });

                                    return p;
                                }

                                return $q(function (resolve) {
                                    $scope.lastSet = $scope.options;
                                    resolve($scope.options);
                                });
                            };
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc component
         * @name tbHiddenField
         * @module tubular.directives
         *
         * @description
         * The `tbHiddenField` component represents a hidden field.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @param {string} name Set the field name.
         * @param {object} value Set the value.
         */
        .component('tbHiddenField', {
            template: '<input type="hidden" ng-model="$ctrl.value" class="form-control" name="{{$ctrl.name}}"  />',
            bindings: {
                value: '=?',
                name: '@'
            },
            controller: [
                'tubularEditorService', '$scope', function (tubularEditorService, $scope) {
                    var $ctrl = this;

                    $ctrl.$onInit = function() {
                        tubularEditorService.setupScope($scope, null, $ctrl, true);
                    };
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbCheckboxField
         * @module tubular.directives
         *
         * @description
         * The `tbCheckboxField` component represents a checkbox field.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @param {string} name Set the field name.
         * @param {object} value Set the value.
         * @param {object} checkedValue Set the checked value.
         * @param {object} uncheckedValue Set the unchecked value.
         * @param {boolean} isEditing Indicate if the field is showing editor.
         * @param {boolean} showLabel Set if the label should be display.
         * @param {string} label Set the field's label otherwise the name is used.
         * @param {string} help Set the help text.
         */
        .component('tbCheckboxField', {
            template: '<div ng-class="{ \'checkbox\' : $ctrl.isEditing, \'has-error\' : !$ctrl.$valid && $ctrl.$dirty() }" class="tubular-checkbox">' +
                '<input type="checkbox" ng-model="$ctrl.value" ng-disabled="$ctrl.readOnly || !$ctrl.isEditing"' +
                'class="tubular-checkbox" id="{{$ctrl.name}}" name="{{$ctrl.name}}" /> ' +
                '<label ng-show="$ctrl.isEditing" for="{{$ctrl.name}}">' +
                '{{$ctrl.label}}' +
                '</label>' +
                '<span class="help-block error-block" ng-show="$ctrl.isEditing" ' +
                'ng-repeat="error in $ctrl.state.$errors">' +
                '{{error}}' +
                '</span>' +
                '<span class="help-block" ng-show="$ctrl.isEditing && $ctrl.help">{{help}}</span>' +
                '</div>',
            bindings: {
                value: '=?',
                isEditing: '=?',
                editorType: '@',
                showLabel: '=?',
                label: '@?',
                required: '=?',
                name: '@',
                readOnly: '=?',
                help: '@?',
                checkedValue: '=?',
                uncheckedValue: '=?'
            },
            controller: [
                'tubularEditorService', '$scope', function (tubularEditorService, $scope) {
                    var $ctrl = this;

                    $ctrl.$onInit = function () {
                        $ctrl.required = false; // overwrite required to false always
                        $ctrl.checkedValue = angular.isDefined($ctrl.checkedValue) ? $ctrl.checkedValue : true;
                        $ctrl.uncheckedValue = angular.isDefined($ctrl.uncheckedValue) ? $ctrl.uncheckedValue : false;

                        tubularEditorService.setupScope($scope, null, $ctrl, true);
                    };
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbTextArea
         * @module tubular.directives
         *
         * @description
         * The `tbTextArea` component represents a textarea field. 
         * Similar to `tbSimpleEditor` but with a `textarea` HTML element instead of `input`.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         *  
         * @param {string} name Set the field name.
         * @param {object} value Set the value.
         * @param {boolean} isEditing Indicate if the field is showing editor.
         * @param {boolean} showLabel Set if the label should be display.
         * @param {string} label Set the field's label otherwise the name is used.
         * @param {string} placeholder Set the placeholder text.
         * @param {string} help Set the help text.
         * @param {boolean} required Set if the field is required.
         * @param {boolean} readOnly Set if the field is read-only.
         * @param {number} min Set the minimum characters.
         * @param {number} max Set the maximum characters.
         */
        .component('tbTextArea', {
            template: '<div ng-class="{ \'form-group\' : $ctrl.showLabel && $ctrl.isEditing, \'has-error\' : !$ctrl.$valid && $ctrl.$dirty() }">' +
                '<span ng-hide="$ctrl.isEditing">{{$ctrl.value}}</span>' +
                '<label ng-show="$ctrl.showLabel">{{ $ctrl.label }}</label>' +
                '<textarea ng-show="$ctrl.isEditing" placeholder="{{$ctrl.placeholder}}" ng-model="$ctrl.value" class="form-control" ' +
                ' ng-required="$ctrl.required" ng-readonly="$ctrl.readOnly" name="{{$ctrl.name}}"></textarea>' +
                '<span class="help-block error-block" ng-show="$ctrl.isEditing" ng-repeat="error in $ctrl.state.$errors">' +
                '{{error}}' +
                '</span>' +
                '<span class="help-block" ng-show="$ctrl.isEditing && $ctrl.help">{{$ctrl.help}}</span>' +
                '</div>',
            bindings: {
                value: '=?',
                isEditing: '=?',
                showLabel: '=?',
                label: '@?',
                placeholder: '@?',
                required: '=?',
                min: '=?',
                max: '=?',
                name: '@',
                readOnly: '=?',
                help: '@?'
            },
            controller: [
                'tubularEditorService', '$scope', '$filter', function (tubularEditorService, $scope, $filter) {
                    var $ctrl = this;

                    $ctrl.validate = function () {
                        if (angular.isDefined($ctrl.min) && angular.isDefined($ctrl.value) && $ctrl.value != null) {
                            if ($ctrl.value.length < parseInt($ctrl.min)) {
                                $ctrl.$valid = false;
                                $ctrl.state.$errors = [$filter('translate')('EDITOR_MIN_CHARS', +$ctrl.min)];
                                return;
                            }
                        }

                        if (angular.isDefined($ctrl.max) && angular.isDefined($ctrl.value) && $ctrl.value != null) {
                            if ($ctrl.value.length > parseInt($ctrl.max)) {
                                $ctrl.$valid = false;
                                $ctrl.state.$errors = [$filter('translate')('EDITOR_MAX_CHARS', +$ctrl.max)];
                                return;
                            }
                        }
                    };


                    $ctrl.$onInit = function() {
                        tubularEditorService.setupScope($scope, null, $ctrl, false);
                    };
                }
            ]
        });
})(window.angular);
(function (angular) {
    'use strict';

    function setupFilter($scope, $element, $compile, $filter, $ctrl) {
        var filterOperators = {
            'string': {
                'None': $filter('translate')('OP_NONE'),
                'Equals': $filter('translate')('OP_EQUALS'),
                'NotEquals': $filter('translate')('OP_NOTEQUALS'),
                'Contains': $filter('translate')('OP_CONTAINS'),
                'NotContains': $filter('translate')('OP_NOTCONTAINS'),
                'StartsWith': $filter('translate')('OP_STARTSWITH'),
                'NotStartsWith': $filter('translate')('OP_NOTSTARTSWITH'),
                'EndsWith': $filter('translate')('OP_ENDSWITH'),
                'NotEndsWith': $filter('translate')('OP_NOTENDSWITH')
            },
            'numeric': {
                'None': $filter('translate')('OP_NONE'),
                'Equals': $filter('translate')('OP_EQUALS'),
                'Between': $filter('translate')('OP_BETWEEN'),
                'Gte': '>=',
                'Gt': '>',
                'Lte': '<=',
                'Lt': '<'
            },
            'date': {
                'None': $filter('translate')('OP_NONE'),
                'Equals': $filter('translate')('OP_EQUALS'),
                'NotEquals': $filter('translate')('OP_NOTEQUALS'),
                'Between': $filter('translate')('OP_BETWEEN'),
                'Gte': '>=',
                'Gt': '>',
                'Lte': '<=',
                'Lt': '<'
            },
            'datetime': {
                'None': $filter('translate')('OP_NONE'),
                'Equals': $filter('translate')('OP_EQUALS'),
                'NotEquals': $filter('translate')('OP_NOTEQUALS'),
                'Between': $filter('translate')('OP_BETWEEN'),
                'Gte': '>=',
                'Gt': '>',
                'Lte': '<=',
                'Lt': '<'
            },
            'datetimeutc': {
                'None': $filter('translate')('OP_NONE'),
                'Equals': $filter('translate')('OP_EQUALS'),
                'NotEquals': $filter('translate')('OP_NOTEQUALS'),
                'Between': $filter('translate')('OP_BETWEEN'),
                'Gte': '>=',
                'Gt': '>',
                'Lte': '<=',
                'Lt': '<'
            },
            'boolean': {
                'None': $filter('translate')('OP_NONE'),
                'Equals': $filter('translate')('OP_EQUALS'),
                'NotEquals': $filter('translate')('OP_NOTEQUALS')
            }
        };

        $ctrl.filter = {
            Text: $ctrl.text || null,
            Argument: $ctrl.argument ? [$ctrl.argument] : null,
            Operator: $ctrl.operator || "Contains",
            OptionsUrl: $ctrl.optionsUrl || null,
            HasFilter: !($ctrl.text == null),
            Name: $scope.$parent.$parent.column.Name
        };

        $ctrl.filterTitle = $ctrl.title || $filter('translate')('CAPTION_FILTER');

        $scope.$watch(function() {
            var columns = $ctrl.$component.columns.filter(function($element) {
                return $element.Name === $ctrl.filter.Name;
            });

            return columns.length !== 0 ? columns[0] : null;
        }, function(val) {
            if (val && val != null) {
                if ($ctrl.filter.HasFilter != val.Filter.HasFilter) {
                    $ctrl.filter.HasFilter = val.Filter.HasFilter;
                    $ctrl.filter.Text = val.Filter.Text;
                    $ctrl.retrieveData();
                }
            }
        }, true);

        $ctrl.retrieveData = function() {
            var columns = $ctrl.$component.columns.filter(function($element) {
                return $element.Name === $ctrl.filter.Name;
            });

            if (columns.length !== 0) {
                columns[0].Filter = $ctrl.filter;
            }

            $ctrl.$component.retrieveData();
            $ctrl.close();
        };

        $ctrl.clearFilter = function() {
            if ($ctrl.filter.Operator !== 'Multiple') {
                $ctrl.filter.Operator = 'None';
            }

            $ctrl.filter.Text = '';
            $ctrl.filter.Argument = [];
            $ctrl.filter.HasFilter = false;
            $ctrl.retrieveData();
        };

        $ctrl.applyFilter = function() {
            $ctrl.filter.HasFilter = true;
            $ctrl.retrieveData();
        };

        $ctrl.close = function() {
            $ctrl.isOpen = false;
        };

        $ctrl.checkEvent = function(keyEvent) {
            if (keyEvent.which === 13) {
                $ctrl.applyFilter();
                keyEvent.preventDefault();
            }
        };

        var columns = $ctrl.$component.columns.filter(function($element) {
            return $element.Name === $ctrl.filter.Name;
        });

        $scope.$watch('$ctrl.filter.Operator', function (val) {
            if (val === 'None') $ctrl.filter.Text = '';
        });

        if (columns.length === 0) return;

        $scope.$watch('$ctrl.filter', function (n) {
            if (columns[0].Filter.Text !== n.Text) {
                n.Text = columns[0].Filter.Text;

                if (columns[0].Filter.Operator !== n.Operator) {
                    n.Operator = columns[0].Filter.Operator;
                }
            }

            $ctrl.filter.HasFilter = columns[0].Filter.HasFilter;
        });

        columns[0].Filter = $ctrl.filter;
        $ctrl.dataType = columns[0].DataType;
        $ctrl.filterOperators = filterOperators[$ctrl.dataType];

        if ($ctrl.dataType === 'date' || $ctrl.dataType === 'datetime' || $ctrl.dataType === 'datetimeutc') {
            $ctrl.filter.Argument = [new Date()];

            if ($ctrl.filter.Operator === 'Contains') {
                $ctrl.filter.Operator = 'Equals';
            }
        }

        if ($ctrl.dataType === 'numeric' || $ctrl.dataType === 'boolean') {
            $ctrl.filter.Argument = [1];

            if ($ctrl.filter.Operator === 'Contains') {
                $ctrl.filter.Operator = 'Equals';
            }
        }
    };

    angular.module('tubular.directives')
         /**
         * @ngdoc component
         * @name tbColumnFilterButtons
         * @module tubular.directives
         *
         * @description
         * The `tbColumnFilterButtons` is an internal component, and it is used to show basic filtering buttons.
         */
        .component('tbColumnFilterButtons', {
            require: {
                $columnFilter: '^?tbColumnFilter',
                $columnDateTimeFilter: '^?tbColumnDateTimeFilter',
                $columnOptionsFilter: '^?tbColumnOptionsFilter'
            },
            template: '<div class="text-right">' +
                      '<a class="btn btn-sm btn-success" ng-click="$ctrl.currentFilter.applyFilter()"' +
                      'ng-disabled="$ctrl.currentFilter.filter.Operator == \'None\'">{{\'CAPTION_APPLY\' | translate}}</a>&nbsp;' +
                      '<button class="btn btn-sm btn-danger" ng-click="$ctrl.currentFilter.clearFilter()">{{\'CAPTION_CLEAR\' | translate}}</button>' +
                      '</div>',
            controller: ['$scope',
                function ($scope) {
                    var $ctrl = this;

                    $ctrl.$onInit = function() {
                        // Set currentFilter to either one of the parent components or for when this template is being rendered by $compile
                        $ctrl.currentFilter = $ctrl.$columnFilter || $ctrl.$columnDateTimeFilter || $ctrl.$columnOptionsFilter || $scope.$parent.$ctrl;
                    };
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbColumnSelector
         * @module tubular.directives
         *
         * @description
         * The `tbColumnSelector` is a button to show columns selector popup.
         */
        .component('tbColumnSelector', {
            require: {
                $component: '^tbGrid'
            },
            template: '<button class="btn btn-sm btn-default" ng-click="$ctrl.openColumnsSelector()">{{\'CAPTION_SELECTCOLUMNS\' | translate}}</button></div>',
            controller: [
                '$scope', '$uibModal', function ($scope, $modal) {
                    var $ctrl = this;

                    $ctrl.openColumnsSelector = function () {
                        var model = $ctrl.$component.columns;

                        var dialog = $modal.open({
                            template: '<div class="modal-header">' +
                                '<h3 class="modal-title">{{\'CAPTION_SELECTCOLUMNS\' | translate}}</h3>' +
                                '</div>' +
                                '<div class="modal-body">' +
                                '<table class="table table-bordered table-responsive table-striped table-hover table-condensed">' +
                                '<thead><tr><th>Visible?</th><th>Name</th></tr></thead>' +
                                '<tbody><tr ng-repeat="col in Model">' +
                                '<td><input type="checkbox" ng-model="col.Visible" ng-disabled="col.Visible && isInvalid()" /></td>' +
                                '<td>{{col.Label}}</td>' +
                                '</tr></tbody></table></div>' +
                                '</div>' +
                                '<div class="modal-footer"><button class="btn btn-warning" ng-click="closePopup()">{{\'CAPTION_CLOSE\' | translate}}</button></div>',
                            backdropClass: 'fullHeight',
                            animation: false,
                            controller: [
                                '$scope', function ($innerScope) {
                                    $innerScope.Model = model;
                                    $innerScope.isInvalid = function () {
                                        return $innerScope.Model.filter(function (el) { return el.Visible; }).length === 1;
                                    }

                                    $innerScope.closePopup = function () {
                                        dialog.close();
                                    };
                                }
                            ]
                        });
                    };
                }
            ]
        })
        /**
         * @ngdoc directive
         * @name tbColumnFilter
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbColumnFilter` directive is a the basic filter popover. You need to define it inside a `tbColumn`.
         * 
         * The parent scope will provide information about the data type.
         * 
         * @param {string} title Set the popover title.
         * @param {string} text Set the search text.
         * @param {string} operator Set the initial operator, default depends on data type.
         * @param {object} argument Set the argument.
         */
        .component('tbColumnFilter', {
            require: {
                $component: '^tbGrid'
            },
            template: '<div class="tubular-column-menu">' +
                '<button class="btn btn-xs btn-default btn-popover" ' +
                'uib-popover-template="$ctrl.templateName" popover-placement="bottom" popover-title="{{$ctrl.filterTitle}}" popover-is-open="$ctrl.isOpen"' +
                ' popover-trigger="click outsideClick" ng-class="{ \'btn-success\': $ctrl.filter.HasFilter }">' +
                '<i class="fa fa-filter"></i></button>' +
                '</div>',
            bindings: {
                text: '@',
                argument: '@',
                operator: '@',
                title: '@'
            },
            controller: [
                '$scope', '$element', '$compile', '$filter', 'tubularTemplateService', function ($scope, $element, $compile, $filter, tubularTemplateService) {
                    var $ctrl = this;

                    $ctrl.$onInit = function () {
                        $ctrl.templateName = tubularTemplateService.tbColumnFilterPopoverTemplateName;
                        setupFilter($scope, $element, $compile, $filter, $ctrl);
                    };
                }
            ]
        })
        /**
         * @ngdoc directive
         * @name tbColumnDateTimeFilter
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbColumnDateTimeFilter` directive is a specific filter with Date and Time editors, instead regular inputs.
         * 
         * The parent scope will provide information about the data type.
         * 
         * @param {string} text Set the search text.
         * @param {object} argument Set the search object (if the search is text use text attribute).
         * @param {string} operator Set the initial operator, default depends on data type.
         */
        .component('tbColumnDateTimeFilter', {
            require: {
                $component: '^tbGrid'
            },
            template: '<div class="tubular-column-menu">' +
                '<button class="btn btn-xs btn-default btn-popover" ' +
                'uib-popover-template="$ctrl.templateName" popover-placement="bottom" popover-title="{{$ctrl.filterTitle}}" popover-is-open="$ctrl.isOpen" '+
                'popover-trigger="ousideClick" ng-class="{ \'btn-success\': $ctrl.filter.HasFilter }">' +
                '<i class="fa fa-filter"></i></button>' +
                '</div>',
            bindings: {
                text: '@',
                argument: '@',
                operator: '@',
                title: '@'
            },
            controller: [
                '$scope', '$element', '$compile', '$filter', 'tubularTemplateService', function ($scope, $element, $compile, $filter, tubularTemplateService) {
                    var $ctrl = this;

                    $ctrl.$onInit = function() {                       
                        $ctrl.templateName = tubularTemplateService.tbColumnDateTimeFilterPopoverTemplateName;
                          setupFilter($scope, $element, $compile, $filter, $ctrl);
                    };
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbColumnOptionsFilter
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbColumnOptionsFilter` directive is a filter with an dropdown listing all the possible values to filter.
         * 
         * @param {object} argument Set the search object.
         * @param {string} operator Set the initial operator, default depends on data type.
         * @param {string} optionsUrl Set the URL to retrieve options
         */
        .component('tbColumnOptionsFilter', {
            require: {
                $component: '^tbGrid'
            },
            template: '<div class="tubular-column-menu">' +
                '<button class="btn btn-xs btn-default btn-popover" uib-popover-template="$ctrl.templateName" popover-placement="bottom" ' +
                'popover-title="{{$ctrl.filterTitle}}" popover-is-open="$ctrl.isOpen" popover-trigger="click outsideClick" ' +
                'ng-class="{ \'btn-success\': $ctrl.filter.HasFilter }">' +
                '<i class="fa fa-filter"></i></button>' +
                '</div>',
            bindings: {
                text: '@',
                argument: '@',
                operator: '@',
                optionsUrl: '@',
                title: '@'
            },
            controller: [
                '$scope', '$element', '$compile', '$filter', 'tubularTemplateService', function ($scope, $element, $compile, $filter, tubularTemplateService) {
                    var $ctrl = this;

                    $ctrl.getOptionsFromUrl = function () {
                        if ($ctrl.dataIsLoaded) {
                            $scope.$apply();
                            return;
                        }

                        var currentRequest = $ctrl.$component.dataService.retrieveDataAsync({
                            serverUrl: $ctrl.filter.OptionsUrl,
                            requestMethod: 'GET'
                        });

                        currentRequest.promise.then(
                            function (data) {
                                $ctrl.optionsItems = data;
                                $ctrl.dataIsLoaded = true;
                            }, function (error) {
                                $scope.$emit('tbGrid_OnConnectionError', error);
                            });
                    };

                    $ctrl.$onInit = function() {
                        $ctrl.dataIsLoaded = false;
                        $ctrl.templateName = tubularTemplateService.tbColumnOptionsFilterPopoverTemplateName;
                        setupFilter($scope, $element, $compile, $filter, $ctrl);
                        $ctrl.getOptionsFromUrl();

                        $ctrl.filter.Operator = 'Multiple';
                    };
                }
            ]
        });
})(window.angular);
(function (angular) {
    'use strict';

    angular.module('tubular.directives')
        /**
         * @ngdoc directive
         * @name tbForm
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbForm` directive is the base to create any form powered by Tubular. Define `dataService` and 
         * `modelKey` to auto-load a record. The `serverSaveUrl` can be used to create a new or update
         * an existing record.
         * 
         * Please don't bind a controller directly to the `tbForm`, Angular will throw an exception. If you want
         * to extend the form behavior put a controller in a upper node like a div.
         * 
         * The `save` method can be forced to update a model against the REST service, otherwise if the Model
         * doesn't detect any change will ignore the save call.
         * 
         * @param {string} serverUrl Set the HTTP URL where the data comes.
         * @param {string} serverSaveUrl Set the HTTP URL where the data will be saved.
         * @param {string} serverSaveMethod Set HTTP Method to save data.
         * @param {object} model The object model to show in the form.
         * @param {string} modelKey Defines the fields to use like Keys.
         * @param {string} formName Defines the form name.
         * @param {string} serviceName Define Data service (name) to retrieve data, defaults `tubularHttp`.
         * @param {bool} requireAuthentication Set if authentication check must be executed, default true.
         */
        .directive('tbForm', ['tubularEditorService',
            function (tubularEditorService) {
                return {
                    template: function (element, attrs) {
                        // Angular Form requires a name for the form
                        // use the provided one or create a unique id for it
                        var name = attrs.name || tubularEditorService.getUniqueTbFormName();
                        return '<form ng-transclude name="' + name + '"></form>';
                    },
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: {
                        model: '=?',
                        serverUrl: '@',
                        serverSaveUrl: '@',
                        serverSaveMethod: '@',
                        modelKey: '@?',
                        dataServiceName: '@?serviceName',
                        requireAuthentication: '=?',
                        name: '@?formName'
                    },
                    controller: [
                        '$scope', '$routeParams', 'tubularModel', 'tubularHttp', '$timeout', '$element',
                        function ($scope, $routeParams, TubularModel, tubularHttp, $timeout, $element) {
                            $scope.tubularDirective = 'tubular-form';
                            $scope.serverSaveMethod = $scope.serverSaveMethod || 'POST';
                            $scope.fields = [];
                            $scope.hasFieldsDefinitions = false;
                            $scope.dataService = tubularHttp.getDataService($scope.dataServiceName);

                            // This method is meant to provide a reference to the Angular Form
                            // so we can get information about: $pristine, $dirty, $submitted, etc.
                            $scope.getFormScope = function () {
                                return $scope[$element.attr('name')];
                            };

                            // Setup require authentication
                            $scope.requireAuthentication = angular.isUndefined($scope.requireAuthentication) ? true : $scope.requireAuthentication;
                            tubularHttp.setRequireAuthentication($scope.requireAuthentication);

                            $scope.$watch('hasFieldsDefinitions', function (newVal) {
                                if (newVal !== true) return;
                                $scope.retrieveData();
                            });

                            $scope.cloneModel = function (model) {
                                var data = {};

                                angular.forEach(model, function (value, key) {
                                    if (key[0] === '$') return;

                                    data[key] = value;
                                });

                                $scope.model = new TubularModel($scope, $scope, data, $scope.dataService);
                                $scope.bindFields();
                            }

                            $scope.bindFields = function () {
                                angular.forEach($scope.fields, function (field) {
                                    field.bindScope();
                                });
                            };

                            $scope.retrieveData = function () {
                                // Try to load a key from markup or route
                                $scope.modelKey = $scope.modelKey || $routeParams.param;

                                if (angular.isDefined($scope.serverUrl)) {
                                    if (angular.isDefined($scope.modelKey) &&
                                        $scope.modelKey != null &&
                                        $scope.modelKey !== '') {
                                        $scope.dataService.getByKey($scope.serverUrl, $scope.modelKey).promise.then(
                                            function (data) {
                                                $scope.model = new TubularModel($scope, $scope, data, $scope.dataService);
                                                $scope.bindFields();
                                            }, function (error) {
                                                $scope.$emit('tbForm_OnConnectionError', error);
                                            });
                                    } else {
                                        $scope.dataService.get(tubularHttp.addTimeZoneToUrl($scope.serverUrl)).promise.then(
                                            function (data) {
                                                var innerScope = $scope;
                                                var dataService = $scope.dataService;

                                                if (angular.isDefined($scope.model) && angular.isDefined($scope.model.$component)) {
                                                    innerScope = $scope.model.$component;
                                                    dataService = $scope.model.$component.dataService;
                                                }

                                                $scope.model = new TubularModel(innerScope, innerScope, data, dataService);
                                                $scope.bindFields();
                                                $scope.model.$isNew = true;
                                            }, function (error) {
                                                $scope.$emit('tbForm_OnConnectionError', error);
                                            });
                                    }

                                    return;
                                }

                                if (angular.isUndefined($scope.model)) {
                                    $scope.model = new TubularModel($scope, $scope, {}, $scope.dataService);
                                }

                                $scope.bindFields();
                            };

                            $scope.save = function (forceUpdate) {
                                if (!$scope.model.$valid()) {
                                    return;
                                }

                                $scope.currentRequest = $scope.model.save(forceUpdate);

                                if ($scope.currentRequest === false) {
                                    $scope.$emit('tbForm_OnSavingNoChanges', $scope);
                                    return;
                                }

                                $scope.currentRequest.then(
                                        function (data) {
                                            if (angular.isDefined($scope.model.$component) &&
                                                angular.isDefined($scope.model.$component.autoRefresh) &&
                                                $scope.model.$component.autoRefresh) {
                                                $scope.model.$component.retrieveData();
                                            }

                                            $scope.$emit('tbForm_OnSuccessfulSave', data, $scope);
                                            $scope.clear();
                                        }, function (error) {
                                            $scope.$emit('tbForm_OnConnectionError', error, $scope);
                                        })
                                    .then(function () {
                                        $scope.model.$isLoading = false;
                                        $scope.currentRequest = null;
                                    });
                            };

                            $scope.update = function (forceUpdate) {
                                $scope.save(forceUpdate);
                            };

                            $scope.create = function () {
                                $scope.model.$isNew = true;
                                $scope.save();
                            };

                            $scope.cancel = function () {
                                $scope.$emit('tbForm_OnCancel', $scope.model);
                                $scope.clear();
                            };

                            $scope.clear = function () {
                                angular.forEach($scope.fields, function (field) {
                                    if (field.resetEditor) {
                                        field.resetEditor();
                                    } else {
                                        field.value = field.defaultValue;
                                    }
                                });
                            };

                            $scope.finishDefinition = function () {
                                var timer = $timeout(function () {
                                    $scope.hasFieldsDefinitions = true;

                                    if ($element.find('input').length) {
                                        $element.find('input')[0].focus();
                                    }
                                }, 0);

                                $scope.$emit('tbForm_OnGreetParentController', $scope);
                                $scope.$on('$destroy', function () { $timeout.cancel(timer); });
                            };
                        }
                    ],
                    compile: function compile() {
                        return {
                            post: function (scope) {
                                scope.finishDefinition();
                            }
                        };
                    }
                };
            }
        ]);
})(window.angular);
(function(angular) {
    'use strict';

    angular.module('tubular.directives')
        /**
         * @ngdoc component
         * @name tbTextSearch
         * @module tubular.directives
         *
         * @description
         * The `tbTextSearch` is visual component to enable free-text search in a grid.
         * 
         * @param {number} minChars How many chars before to search, default 3.
         * @param {string} placeholder The placeholder text, defaults `UI_SEARCH` i18n resource.
         */
        .component('tbTextSearch', {
            require: {
                $component: '^tbGrid'
            },
            template:
                '<div class="tubular-grid-search">' +
                    '<div class="input-group input-group-sm">' +
                    '<span class="input-group-addon"><i class="fa fa-search"></i></span>' +
                    '<input type="search" class="form-control" placeholder="{{:: $ctrl.placeholder || (\'UI_SEARCH\' | translate) }}" maxlength="20" ' +
                    'ng-model="$ctrl.$component.search.Text" ng-model-options="{ debounce: 300 }">' +
                    '<span class="input-group-btn" ng-show="$ctrl.$component.search.Text.length > 0">' +
                    '<button class="btn btn-default" uib-tooltip="{{\'CAPTION_CLEAR\' | translate}}" ng-click="$ctrl.$component.search.Text = \'\'">' +
                    '<i class="fa fa-times-circle"></i>' +
                    '</button>' +
                    '</span>' +
                    '<div>' +
                    '<div>',
            bindings: {
                minChars: '@?',
                placeholder: '@'
            },
            controller: [
                '$scope', function($scope) {
                    var $ctrl = this;

                    $ctrl.$onInit = function() {
                        $ctrl.minChars = $ctrl.minChars || 3;
                        $ctrl.lastSearch = $ctrl.$component.search.Text;
                    };

                    $scope.$watch("$ctrl.$component.search.Text", function (val, prev) {
                        if (angular.isUndefined(val) || val === prev) {
                            return;
                        }

                        $ctrl.$component.search.Text = val;

                        if ($ctrl.lastSearch !== "" && val === "") {
                            $ctrl.$component.saveSearch();
                            $ctrl.$component.search.Operator = 'None';
                            $ctrl.$component.retrieveData();
                            return;
                        }

                        if (val === "" || val.length < $ctrl.minChars) {
                            return;
                        }

                        if (val === $ctrl.lastSearch) {
                            return;
                        }

                        $ctrl.lastSearch = val;
                        $ctrl.$component.saveSearch();
                        $ctrl.$component.search.Operator = 'Auto';
                        $ctrl.$component.retrieveData();
                    });
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbRemoveButton
         * @module tubular.directives
         *
         * @description
         * The `tbRemoveButton` component is visual helper to show a Remove button with a popover to confirm the action.
         * 
         * @param {object} model The row to remove.
         * @param {string} caption Set the caption to use in the button, default Remove.
         * @param {string} cancelCaption Set the caption to use in the Cancel button, default `CAPTION_REMOVE` i18n resource.
         * @param {string} legend Set the legend to warn user, default `UI_REMOVEROW` i18n resource.
         * @param {string} icon Set the CSS icon's class, the button can have only icon.
         */
        .component('tbRemoveButton', {
            require: '^tbGrid',
            template: '<button class="btn btn-danger btn-xs btn-popover" uib-popover-template="$ctrl.templateName" popover-placement="right" ' +
                'popover-title="{{ $ctrl.legend || (\'UI_REMOVEROW\' | translate) }}" popover-is-open="$ctrl.isOpen" popover-trigger="click outsideClick" ' +
                'ng-hide="$ctrl.model.$isEditing">' +
                '<span ng-show="$ctrl.showIcon" class="{{::icon}}"></span>' +
                '<span ng-show="$ctrl.showCaption">{{:: $ctrl.caption || (\'CAPTION_REMOVE\' | translate) }}</span>' +
                '</button>',
            bindings: {
                model: '=',
                caption: '@',
                cancelCaption: '@',
                legend: '@',
                icon: '@'
            },
            controller: [
               'tubularTemplateService', function (tubularTemplateService) {
                   var $ctrl = this;

                   $ctrl.showIcon = angular.isDefined($ctrl.icon);
                   $ctrl.showCaption = !($ctrl.showIcon && angular.isUndefined($ctrl.caption));

                   $ctrl.$onInit = function () {
                       $ctrl.templateName = tubularTemplateService.tbRemoveButtonrPopoverTemplateName;
                   }
               }
            ]
        })
        /**
         * @ngdoc directive
         * @name tbSaveButton
         * @module tubular.directives
         * @restrict E
         *
         * @description
         * The `tbSaveButton` directive is visual helper to show a Save button and Cancel button.
         * 
         * @scope
         * 
         * @param {object} model The row to remove.
         * @param {boolean} isNew Set if the row is a new record.
         * @param {string} saveCaption Set the caption to use in Save the button, default Save.
         * @param {string} saveCss Add a CSS class to Save button.
         * @param {string} cancelCaption Set the caption to use in cancel the button, default Cancel.
         * @param {string} cancelCss Add a CSS class to Cancel button.
         */
        .directive('tbSaveButton', [
            function() {

                return {
                    require: '^tbGrid',
                    template: '<div ng-show="model.$isEditing">' +
                        '<button ng-click="save()" class="btn btn-default {{:: saveCss || \'\' }}" ' +
                        'ng-disabled="!model.$valid()">' +
                        '{{:: saveCaption || (\'CAPTION_SAVE\' | translate) }}' +
                        '</button>' +
                        '<button ng-click="cancel()" class="btn {{:: cancelCss || \'btn-default\' }}">' +
                        '{{:: cancelCaption || (\'CAPTION_CANCEL\' | translate) }}' +
                        '</button></div>',
                    restrict: 'E',
                    replace: true,
                    scope: {
                        model: '=',
                        isNew: '=?',
                        saveCaption: '@',
                        saveCss: '@',
                        cancelCaption: '@',
                        cancelCss: '@'
                    },
                    controller: [
                        '$scope', function($scope) {
                            $scope.isNew = $scope.isNew || false;

                            $scope.save = function() {
                                if ($scope.isNew) {
                                    $scope.model.$isNew = true;
                                }

                                if (!$scope.model.$valid()) {
                                    return;
                                }

                                $scope.currentRequest = $scope.model.save();

                                if ($scope.currentRequest === false) {
                                    $scope.$emit('tbGrid_OnSavingNoChanges', $scope.model);
                                    return;
                                }

                                $scope.currentRequest.then(
                                    function(data) {
                                        $scope.model.$isEditing = false;

                                        if (angular.isDefined($scope.model.$component) &&
                                            angular.isDefined($scope.model.$component.autoRefresh) &&
                                            $scope.model.$component.autoRefresh) {
                                            $scope.model.$component.retrieveData();
                                        }

                                        $scope.$emit('tbGrid_OnSuccessfulSave', data, $scope.model.$component);
                                    }, function(error) {
                                        $scope.$emit('tbGrid_OnConnectionError', error);
                                    });
                            };

                            $scope.cancel = function() {
                                $scope.model.revertChanges();
                            };
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc component
         * @name tbEditButton
         * @module tubular.directives
         *
         * @description
         * The `tbEditButton` component is visual helper to create an Edit button.
         * 
         * @param {object} model The row to remove.
         * @param {string} caption Set the caption to use in the button, default Edit.
         */
        .component('tbEditButton', {
            require: {
                $component: '^tbGrid'
            },
            template: '<button ng-click="$ctrl.edit()" class="btn btn-xs btn-default" ' +
                'ng-hide="$ctrl.model.$isEditing">{{:: $ctrl.caption || (\'CAPTION_EDIT\' | translate) }}</button>',
            bindings: {
                model: '=',
                caption: '@'
            },
            controller: [
                '$scope', function($scope) {
                    var $ctrl = this;

                    $ctrl.edit = function() {
                        if ($ctrl.$component.editorMode === 'popup') {
                            $ctrl.model.editPopup();
                        } else {
                            $ctrl.model.edit();
                        }
                    };
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbPageSizeSelector
         * @module tubular.directives
         *
         * @description
         * The `tbPageSizeSelector` component is visual helper to render a dropdown to allow user select how many rows by page.
         * 
         * @param {string} caption Set the caption to use in the button, default "Page size:".
         * @param {string} css Add a CSS class to the `div` HTML element.
         * @param {string} selectorCss Add a CSS class to the `select` HTML element.
         * @param {array} options Set the page options array, default [10, 20, 50, 100].
         */
        .component('tbPageSizeSelector', {
            require: {
                $component: '^tbGrid'
            },
            template: '<div class="{{::$ctrl.css}}"><form class="form-inline">' +
                '<div class="form-group">' +
                '<label class="small">{{:: $ctrl.caption || (\'UI_PAGESIZE\' | translate) }} </label>&nbsp;' +
                '<select ng-model="$ctrl.$component.pageSize" class="form-control input-sm {{::$ctrl.selectorCss}}" ' +
                'ng-options="item for item in options">' +
                '</select>' +
                '</div>' +
                '</form></div>',
            bindings: {
                caption: '@',
                css: '@',
                selectorCss: '@',
                options: '=?'
            },
            controller: [
                '$scope', function($scope) {
                    $scope.options = angular.isDefined($scope.$ctrl.options) ? $scope.$ctrl.options : [10, 20, 50, 100];
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbExportButton
         * @module tubular.directives
         *
         * @description
         * The `tbExportButton` component is visual helper to render a button to export grid to CSV format.
         * 
         * @param {string} filename Set the export file name.
         * @param {string} css Add a CSS class to the `button` HTML element.
         * @param {string} caption Set the caption.
         * @param {string} captionMenuCurrent Set the caption.
         * @param {string} captionMenuAll Set the caption.
         */
        .component('tbExportButton', {
            require: {
                $component: '^tbGrid'
            },
            template: '<div class="btn-group" uib-dropdown>' +
                '<button class="btn btn-info btn-sm {{::$ctrl.css}}" uib-dropdown-toggle>' +
                '<span class="fa fa-download"></span>&nbsp;{{:: $ctrl.caption || (\'UI_EXPORTCSV\' | translate)}}&nbsp;<span class="caret"></span>' +
                '</button>' +
                '<ul class="dropdown-menu" uib-dropdown-menu>' +
                '<li><a href="javascript:void(0)" ng-click="$ctrl.downloadCsv($parent)">{{:: $ctrl.captionMenuCurrent || (\'UI_CURRENTROWS\' | translate)}}</a></li>' +
                '<li><a href="javascript:void(0)" ng-click="$ctrl.downloadAllCsv($parent)">{{:: $ctrl.captionMenuAll || (\'UI_ALLROWS\' | translate)}}</a></li>' +
                '</ul>' +
                '</div>',
            bindings: {
                filename: '@',
                css: '@',
                caption: '@',
                captionMenuCurrent: '@',
                captionMenuAll: '@'
            },
            controller: [
                '$scope', 'tubularGridExportService', function ($scope, tubularGridExportService) {
                    var $ctrl = this;

                    $ctrl.downloadCsv = function () {
                        tubularGridExportService.exportGridToCsv($ctrl.filename, $ctrl.$component);
                    };

                    $ctrl.downloadAllCsv = function () {
                        tubularGridExportService.exportAllGridToCsv($ctrl.filename, $ctrl.$component);
                    };
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbPrintButton
         * @module tubular.directives
         *
         * @description
         * The `tbPrintButton` component is visual helper to render a button to print the `tbGrid`.
         * 
         * @param {string} title Set the document's title.
         * @param {string} printCss Set a stylesheet URL to attach to print mode.
         * @param {string} caption Set the caption.
         */
        .component('tbPrintButton', {
            require: {
                $component: '^tbGrid'
            },
            template: '<button class="btn btn-default btn-sm" ng-click="$ctrl.printGrid()">' +
                '<span class="fa fa-print"></span>&nbsp;{{$ctrl.caption || (\'CAPTION_PRINT\' | translate)}}' +
                '</button>',
            bindings: {
                title: '@',
                printCss: '@',
                caption: '@'
            },
            controller: function() {
                var $ctrl = this;

                $ctrl.printGrid = function() {
                    $ctrl.$component.getFullDataSource(function(data) {
                        var tableHtml = "<table class='table table-bordered table-striped'><thead><tr>"
                            + $ctrl.$component.columns
                            .filter(function(c) { return c.Visible; })
                            .map(function(el) {
                                return "<th>" + (el.Label || el.Name) + "</th>";
                            }).join(" ")
                            + "</tr></thead>"
                            + "<tbody>"
                            + data.map(function(row) {
                                if (typeof (row) === 'object') {
                                    row = Object.keys(row).map(function (key) { return row[key] });
                                }

                                return "<tr>" + row.map(function(cell, index) {
                                    if (angular.isDefined($ctrl.$component.columns[index]) &&
                                        !$ctrl.$component.columns[index].Visible) {
                                        return "";
                                    }

                                    return "<td>" + cell + "</td>";
                                }).join(" ") + "</tr>";
                            }).join(" ")
                            + "</tbody>"
                            + "</table>";

                        var popup = window.open("about:blank", "Print", "menubar=0,location=0,height=500,width=800");
                        popup.document.write('<link rel="stylesheet" href="//cdn.jsdelivr.net/bootstrap/latest/css/bootstrap.min.css" />');

                        if ($ctrl.printCss != '') {
                            popup.document.write('<link rel="stylesheet" href="' + $ctrl.printCss + '" />');
                        }

                        popup.document.write('<body onload="window.print();">');
                        popup.document.write('<h1>' + $ctrl.title + '</h1>');
                        popup.document.write(tableHtml);
                        popup.document.write('</body>');
                        popup.document.close();
                    });
                };
            }
        });
})(window.angular);
(function (angular) {
    'use strict';

    angular.module('tubular.directives')
        /**
         * @ngdoc component
         * @name tbGridPager
         * @module tubular.directives
         *
         * @description
         * The `tbGridPager` component generates a pager connected to the parent `tbGrid`.
         */
        .component('tbGridPager', {
            require: {
                $component : '^tbGrid'
            },
            template:
                '<div class="tubular-pager">' +
                    '<uib-pagination ng-disabled="$ctrl.$component.isEmpty" direction-links="true" ' +
                    'first-text="&#xf049;" previous-text="&#xf04a;" next-text="&#xf04e;" last-text="&#xf050;"' +
                    'boundary-links="true" total-items="$ctrl.$component.filteredRecordCount" ' +
                    'items-per-page="$ctrl.$component.pageSize" max-size="5" ng-model="$ctrl.$component.currentPage" ng-change="$ctrl.pagerPageChanged()">' +
                    '</uib-pagination>' +
                    '<div>',
            scope: true,
            terminal: false,
            controller: ['$scope', function ($scope) {
                    var $ctrl = this;

                    $scope.$watch('$ctrl.$component.currentPage', function () {
                        if ($ctrl.$component.currentPage !== $ctrl.$component.requestedPage) {
                            $ctrl.$component.requestedPage = $ctrl.$component.currentPage;
                        }
                    });

                    $ctrl.pagerPageChanged = function () {
                        $ctrl.$component.requestedPage = $ctrl.$component.currentPage;
                    };
                }
            ]
        })
        /**
         * @ngdoc component
         * @name tbGridPagerInfo
         * @module tubular.directives
         *
         * @description
         * The `tbGridPagerInfo` component shows how many records are shown in a page and total rows.
         */
        .component('tbGridPagerInfo', {
            require: {
                $component: '^tbGrid'
            },
            template: '<div class="pager-info small" ng-hide="$ctrl.$component.isEmpty">' +
                '{{\'UI_SHOWINGRECORDS\' | translate: $ctrl.currentInitial:$ctrl.currentTop:$ctrl.$component.filteredRecordCount}} ' +
                '<span ng-show="$ctrl.filtered">' +
                '{{\'UI_FILTEREDRECORDS\' | translate: $ctrl.$component.totalRecordCount}}</span>' +
                '</div>',
            bindings: {
                cssClass: '@?'
            },
            controller: [
                '$scope', function ($scope) {
                    var $ctrl = this;

                    $ctrl.fixCurrentTop = function () {
                        $ctrl.currentTop = $ctrl.$component.pageSize * $ctrl.$component.currentPage;
                        $ctrl.currentInitial = (($ctrl.$component.currentPage - 1) * $ctrl.$component.pageSize) + 1;

                        if ($ctrl.currentTop > $ctrl.$component.filteredRecordCount) {
                            $ctrl.currentTop = $ctrl.$component.filteredRecordCount;
                        }

                        if ($ctrl.currentTop < 0) {
                            $ctrl.currentTop = 0;
                        }

                        if ($ctrl.currentInitial < 0 || $ctrl.$component.totalRecordCount === 0) {
                            $ctrl.currentInitial = 0;
                        }
                    };

                    $scope.$watch('$ctrl.$component.filteredRecordCount', function () {
                        $ctrl.filtered = $ctrl.$component.totalRecordCount != $ctrl.$component.filteredRecordCount;
                        $ctrl.fixCurrentTop();
                    });

                    $scope.$watch('$ctrl.$component.currentPage', function () {
                        $ctrl.fixCurrentTop();
                    });

                    $scope.$watch('$ctrl.$component.pageSize', function () {
                        $ctrl.fixCurrentTop();
                    });

                    $ctrl.$onInit = function () {
                        $ctrl.fixCurrentTop();
                    };
                }
            ]
        });
})(window.angular);
(function (angular) {
    'use strict';

    /**                                           
    * @ngdoc module
    * @name tubular.models
    * 
    * @description
    * Tubular Models module. 
    * 
    * It contains model's factories to be use in {@link tubular.directives} like `tubularModel` and `tubularGridColumnModel`.
    */
    angular.module('tubular.models', [])
        /**
        * @ngdoc factory
        * @name tubularModel
        * @module tubular.models
        *
        * @description
        * The `tubularModel` factory is the base to generate a row model to use with `tbGrid` and `tbForm`.
        */
        .factory('tubularModel', function() {
            return function($scope, $ctrl, data, dataService) {
                var obj = {
                    $key: "",
                    $addField: function(key, value, ignoreOriginal) {
                        this[key] = value;
                        if (angular.isUndefined(this.$original)) {
                            this.$original = {};
                        }

                        this.$original[key] = ignoreOriginal ? undefined : value;

                        if (ignoreOriginal) {
                            this.$hasChanges = true;
                        }

                        if (angular.isUndefined(this.$state)) {
                            this.$state = {};
                        }

                        $scope.$watch(function() {
                            return obj[key];
                        }, function(newValue, oldValue) {
                            if (newValue === oldValue) return;
                            obj.$hasChanges = obj[key] !== obj.$original[key];
                        });
                    }
                };

                if (angular.isArray(data) === false) {
                    angular.forEach(Object.keys(data), function(name) {
                        obj.$addField(name, data[name]);
                    });
                }

                if (angular.isDefined($ctrl.columns)) {
                    angular.forEach($ctrl.columns, function(col, key) {
                        var value = angular.isDefined(data[key]) ? data[key] : data[col.Name];

                        if (angular.isUndefined(value) && data[key] === 0) {
                            value = 0;
                        }

                        obj.$addField(col.Name, value);

                        if (col.DataType === "date" || col.DataType === "datetime" || col.DataType === "datetimeutc") {
                            if (typeof moment == 'function') {
                                if (col.DataType === "datetimeutc") {
                                    obj[col.Name] = moment.utc(obj[col.Name]);
                                } else {
                                    obj[col.Name] = moment(obj[col.Name]);
                                }
                            } else {
                                var timezone = new Date().toString().match(/([-\+][0-9]+)\s/)[1];
                                timezone = timezone.substr(0, timezone.length - 2) + ':' + timezone.substr(timezone.length - 2, 2);
                                var tempDate = new Date(Date.parse(obj[col.Name] + timezone));

                                if (col.DataType === "date") {
                                    obj[col.Name] = new Date(1900 + tempDate.getYear(), tempDate.getMonth(), tempDate.getDate());
                                } else {
                                    obj[col.Name] = new Date(1900 + tempDate.getYear(),
                                        tempDate.getMonth(), tempDate.getDate(), tempDate.getHours(),
                                        tempDate.getMinutes(), tempDate.getSeconds(), 0);
                                }
                            }
                        }

                        if (col.IsKey) {
                            obj.$key += obj[col.Name] + ",";
                        }
                    });
                }

                if (obj.$key.length > 1) {
                    obj.$key = obj.$key.substring(0, obj.$key.length - 1);
                }

                obj.$isEditing = false;
                obj.$hasChanges = false;
                obj.$selected = false;
                obj.$isNew = false;

                obj.$valid = function() {
                    var valid = true;

                    angular.forEach(obj.$state, function(val) {
                        if (angular.isUndefined(val) || !val.$valid() || !val.$dirty()) {
                            valid = false;
                        }
                    });

                    return valid;
                };

                // Returns a save promise
                obj.save = function(forceUpdate) {
                    if (angular.isUndefined(dataService) || dataService == null) {
                        throw 'Define DataService to your model.';
                    }

                    if (angular.isUndefined($ctrl.serverSaveUrl) || $ctrl.serverSaveUrl == null) {
                        throw 'Define a Save URL.';
                    }

                    if (!forceUpdate && !obj.$isNew && !obj.$hasChanges) {
                        return false;
                    }

                    obj.$isLoading = true;

                    return dataService.saveDataAsync(obj, {
                        serverUrl: $ctrl.serverSaveUrl,
                        requestMethod: obj.$isNew ? ($ctrl.serverSaveMethod || 'POST') : 'PUT'
                    }).promise;
                };

                obj.edit = function() {
                    if (obj.$isEditing && obj.$hasChanges) {
                        obj.save();
                    }

                    obj.$isEditing = !obj.$isEditing;
                };

                obj.delete = function() {
                    $ctrl.deleteRow(obj);
                };

                obj.resetOriginal = function() {
                    for (var k in obj.$original) {
                        if (obj.$original.hasOwnProperty(k)) {
                            obj.$original[k] = obj[k];
                        }
                    }
                };

                obj.revertChanges = function() {
                    for (var k in obj) {
                        if (obj.hasOwnProperty(k)) {
                            if (k[0] === '$' || angular.isUndefined(obj.$original[k])) {
                                continue;
                            }

                            obj[k] = obj.$original[k];
                        }
                    }

                    obj.$isEditing = false;
                    obj.$hasChanges = false;
                };

                return obj;
            };
        });
})(window.angular);
(function (angular) {
    'use strict';

    /**
     * @ngdoc module
     * @name tubular.services
     * 
     * @description
     * Tubular Services module. 
     * It contains common services like Http and OData clients, and filtering and printing services.
     */
    angular.module('tubular.services', ['ui.bootstrap'])
        /**
         * @ngdoc service
         * @name tubularPopupService
         *
         * @description
         * Use `tubularPopupService` to show or generate popups with a `tbForm` inside.
         */
        .service('tubularPopupService', [
            '$uibModal', '$rootScope', 'tubularTemplateService',
            function tubularPopupService($modal, $rootScope, tubularTemplateService) {
                var me = this;

                me.onSuccessForm = function (callback) {
                    $rootScope.$on('tbForm_OnSuccessfulSave', callback);
                };

                me.onConnectionError = function (callback) {
                    $rootScope.$on('tbForm_OnConnectionError', callback);
                };

                /**
                 * Opens a new Popup
                 * @param {string} template 
                 * @param {object} model 
                 * @param {object} gridScope 
                 * @param {string} size 
                 * @returns {object} The Popup instance
                 */
                me.openDialog = function (template, model, gridScope, size) {
                    if (angular.isUndefined(template)) {
                        template = tubularTemplateService.generatePopup(model);
                    }

                    var dialog = $modal.open({
                        templateUrl: template,
                        backdropClass: 'fullHeight',
                        animation: false,
                        size: size,
                        controller: [
                            '$scope', function ($scope) {
                                $scope.Model = model;

                                $scope.savePopup = function (innerModel, forceUpdate) {
                                    innerModel = innerModel || $scope.Model;

                                    // If we have nothing to save and it's not a new record, just close
                                    if (!forceUpdate && !innerModel.$isNew && !innerModel.$hasChanges) {
                                        $scope.closePopup();
                                        return null;
                                    }

                                    var result = innerModel.save(forceUpdate);

                                    if (angular.isUndefined(result) || result === false) {
                                        return null;
                                    }

                                    result.then(
                                        function (data) {
                                            $scope.$emit('tbForm_OnSuccessfulSave', data);
                                            $rootScope.$broadcast('tbForm_OnSuccessfulSave', data);
                                            $scope.Model.$isLoading = false;
                                            if (gridScope.autoRefresh) gridScope.retrieveData();
                                            dialog.close();

                                            return data;
                                        }, function (error) {
                                            $scope.$emit('tbForm_OnConnectionError', error);
                                            $rootScope.$broadcast('tbForm_OnConnectionError', error);
                                            $scope.Model.$isLoading = false;

                                            return error;
                                        });

                                    return result;
                                };

                                $scope.closePopup = function () {
                                    if (angular.isDefined($scope.Model.revertChanges)) {
                                        $scope.Model.revertChanges();
                                    }

                                    dialog.close();
                                };
                            }
                        ]
                    });

                    return dialog;
                };
            }
        ])
        /**
         * @ngdoc service
         * @name tubularGridExportService
         *
         * @description
         * Use `tubularGridExportService` to export your `tbGrid` to a CSV file.
         */
        .service('tubularGridExportService', function tubularGridExportService() {
            var me = this;

            me.getColumns = function (gridScope) {
                return gridScope.columns.map(function (c) { return c.Label; });
            };

            me.getColumnsVisibility = function (gridScope) {
                return gridScope.columns
                    .map(function (c) { return c.Visible; });
            };

            me.exportAllGridToCsv = function (filename, gridScope) {
                var columns = me.getColumns(gridScope);
                var visibility = me.getColumnsVisibility(gridScope);

                gridScope.getFullDataSource(function (data) {
                    me.exportToCsv(filename, columns, data, visibility);
                });
            };

            me.exportGridToCsv = function (filename, gridScope) {
                var columns = me.getColumns(gridScope);
                var visibility = me.getColumnsVisibility(gridScope);

                gridScope.currentRequest = {};
                me.exportToCsv(filename, columns, gridScope.dataSource.Payload, visibility);
                gridScope.currentRequest = null;
            };

            me.exportToCsv = function (filename, header, rows, visibility) {
                var processRow = function (row) {
                    if (typeof (row) === 'object') {
                        row = Object.keys(row).map(function (key) { return row[key]; });
                    }

                    var finalVal = '';
                    for (var j = 0; j < row.length; j++) {
                        if (!visibility[j]) {
                            continue;
                        }

                        var innerValue = row[j] == null ? '' : row[j].toString();

                        if (row[j] instanceof Date) {
                            innerValue = row[j].toLocaleString();
                        }

                        var result = innerValue.replace(/"/g, '""');

                        if (result.search(/("|,|\n)/g) >= 0) {
                            result = '"' + result + '"';
                        }

                        if (j > 0) {
                            finalVal += ',';
                        }

                        finalVal += result;
                    }
                    return finalVal + '\n';
                };

                var csvFile = '';

                if (header.length > 0) {
                    csvFile += processRow(header);
                }

                for (var i = 0; i < rows.length; i++) {
                    csvFile += processRow(rows[i]);
                }

                // Add "\uFEFF" (UTF-8 BOM)
                var blob = new Blob(["\uFEFF" + csvFile], { type: 'text/csv;charset=utf-8;' });
                window.saveAs(blob, filename);
            };
        })
        /**
         * @ngdoc service
         * @name tubularEditorService
         *
         * @description
         * The `tubularEditorService` service is a internal helper to setup any `TubularModel` with a UI.
         */
        .service('tubularEditorService', [
            '$filter', function tubularEditorService($filter) {
                var me = this;

                /**
                * Simple helper to generate a unique name for Tubular Forms
                */
                me.getUniqueTbFormName = function () {
                    // TODO: Maybe move this to another service
                    window.tbFormCounter = window.tbFormCounter || (window.tbFormCounter = -1);
                    window.tbFormCounter++;
                    return "tbForm" + window.tbFormCounter;
                };

                /**
                 * Setups a new Editor, this functions is like a common class constructor to be used
                 * with all the tubularEditors.
                 */
                me.setupScope = function (scope, defaultFormat, ctrl, setDirty) {
                    if (angular.isUndefined(ctrl)) ctrl = scope;

                    ctrl.isEditing = angular.isUndefined(ctrl.isEditing) ? true : ctrl.isEditing;
                    ctrl.showLabel = ctrl.showLabel || false;
                    ctrl.label = ctrl.label || (ctrl.name || '').replace(/([a-z])([A-Z])/g, '$1 $2');
                    ctrl.required = ctrl.required || false;
                    ctrl.readOnly = ctrl.readOnly || false;
                    ctrl.format = ctrl.format || defaultFormat;
                    ctrl.$valid = true;

                    // Get the field reference using the Angular way
                    ctrl.getFormField = function () {
                        var parent = scope.$parent;

                        while (true) {
                            if (parent == null) break;
                            if (angular.isDefined(parent.tubularDirective) && parent.tubularDirective === 'tubular-form') {
                                var formScope = parent.getFormScope();

                                return formScope == null ? null : formScope[scope.Name];
                            }

                            parent = parent.$parent;
                        }
                    };

                    ctrl.$dirty = function () {
                        // Just forward the property
                        var formField = ctrl.getFormField();

                        return formField == null ? true : formField.$dirty;
                    };

                    ctrl.checkValid = function () {
                        ctrl.$valid = true;
                        ctrl.state.$errors = [];

                        if ((angular.isUndefined(ctrl.value) && ctrl.required) ||
                        (Object.prototype.toString.call(ctrl.value) === "[object Date]" && isNaN(ctrl.value.getTime()) && ctrl.required)) {
                            ctrl.$valid = false;

                            // Although this property is invalid, if it is not $dirty
                            // then there should not be any errors for it
                            if (ctrl.$dirty()) {
                                ctrl.state.$errors = [$filter('translate')('EDITOR_REQUIRED')];
                            }

                            if (angular.isDefined(scope.$parent.Model)) {
                                scope.$parent.Model.$state[scope.Name] = ctrl.state;
                            }

                            return;
                        }

                        // Check if we have a validation function, otherwise return
                        if (angular.isUndefined(ctrl.validate)) {
                            return;
                        }

                        ctrl.validate();
                    };

                    scope.$watch(function () {
                        return ctrl.value;
                    }, function (newValue, oldValue) {
                        if (angular.isUndefined(oldValue) && angular.isUndefined(newValue)) {
                            return;
                        }

                        // This is the state API for every property in the Model
                        ctrl.state = {
                            $valid: function () {
                                ctrl.checkValid();
                                return this.$errors.length === 0;
                            },
                            $dirty: function () {
                                return ctrl.$dirty;
                            },
                            $errors: []
                        };

                        ctrl.$valid = true;

                        // Try to match the model to the parent, if it exists
                        if (angular.isDefined(scope.$parent.Model)) {
                            if (angular.isDefined(scope.$parent.Model[ctrl.name])) {
                                scope.$parent.Model[ctrl.name] = newValue;

                                if (angular.isUndefined(scope.$parent.Model.$state)) {
                                    scope.$parent.Model.$state = [];
                                }

                                scope.$parent.Model.$state[scope.Name] = ctrl.state;
                            } else if (angular.isDefined(scope.$parent.Model.$addField)) {
                                scope.$parent.Model.$addField(ctrl.name, newValue, true);
                            }
                        }

                        ctrl.checkValid();
                    });

                    var parent = scope.$parent;

                    // We try to find a Tubular Form in the parents
                    while (true) {
                        if (parent == null) break;
                        if (angular.isDefined(parent.tubularDirective) &&
                        (parent.tubularDirective === 'tubular-form' ||
                            parent.tubularDirective === 'tubular-rowset')) {

                            if (ctrl.name === null) {
                                return;
                            }

                            if (parent.hasFieldsDefinitions !== false) {
                                throw 'Cannot define more fields. Field definitions have been sealed';
                            }

                            ctrl.$component = parent.tubularDirective === 'tubular-form' ? parent : parent.$component;

                            scope.Name = ctrl.name;

                            ctrl.bindScope = function () {
                                scope.$parent.Model = parent.model;

                                if (angular.equals(ctrl.value, parent.model[scope.Name]) === false) {
                                    if (angular.isDefined(parent.model[scope.Name])) {
                                        ctrl.value = (ctrl.DataType === 'date' && parent.model[ctrl.Name] != null) ?
                                            new Date(parent.model[scope.Name]) :
                                            parent.model[scope.Name];
                                    }

                                    parent.$watch(function () {
                                        return ctrl.value;
                                    }, function (value) {
                                        parent.model[scope.Name] = value;
                                    });
                                }

                                if ((!ctrl.value || ctrl.value == null) && (ctrl.defaultValue && ctrl.defaultValue != null)) {
                                    if (ctrl.DataType === 'date' && ctrl.defaultValue != null) {
                                        ctrl.defaultValue = new Date(ctrl.defaultValue);
                                    }
                                    if (ctrl.DataType === 'numeric' && ctrl.defaultValue != null) {
                                        ctrl.defaultValue = parseFloat(ctrl.defaultValue);
                                    }

                                    ctrl.value = ctrl.defaultValue;
                                }

                                if (angular.isUndefined(parent.model.$state)) {
                                    parent.model.$state = {};
                                }

                                // This is the state API for every property in the Model
                                parent.model.$state[scope.Name] = {
                                    $valid: function () {
                                        ctrl.checkValid();
                                        return this.$errors.length === 0;
                                    },
                                    $dirty: function () {
                                        return ctrl.$dirty();
                                    },
                                    $errors: []
                                };

                                if (angular.equals(ctrl.state, parent.model.$state[scope.Name]) === false) {
                                    ctrl.state = parent.model.$state[scope.Name];
                                }

                                if (setDirty) {
                                    var formScope = ctrl.getFormField();
                                    if (formScope) formScope.$setDirty();
                                }
                            };

                            parent.fields.push(ctrl);

                            break;
                        }

                        parent = parent.$parent;
                    }
                };

                /**
                 * True if browser has support for HTML5 date input.
                 */
                me.canUseHtml5Date = function () {
                    // TODO: Remove dup!
                    var input = document.createElement('input');
                    input.setAttribute('type', 'date');

                    var notADateValue = 'not-a-date';
                    input.setAttribute('value', notADateValue);

                    return (input.value !== notADateValue);
                }();
            }
        ]);
})(window.angular);
(function(angular) {
    'use strict';

    angular.module('tubular.services')
        /**
         * @ngdoc service
         * @name tubularHttp
         *
         * @description
         * Use `tubularHttp` to connect a grid or a form to a HTTP Resource. Internally this service is
         * using `$http` to make all the request.
         * 
         * This service provides authentication using bearer-tokens. Based on https://bitbucket.org/david.antaramian/so-21662778-spa-authentication-example
         */
        .service('tubularHttp', [
            '$http', '$timeout', '$q', '$cacheFactory', 'localStorageService', '$filter',
            function tubularHttp($http, $timeout, $q, $cacheFactory, localStorageService, $filter) {
                var me = this;

                function isAuthenticationExpired(expirationDate) {
                    var now = new Date();
                    expirationDate = new Date(expirationDate);

                    return expirationDate - now <= 0;
                }

                function removeData() {
                    localStorageService.remove('auth_data');
                }

                function saveData() {
                    removeData();
                    localStorageService.set('auth_data', me.userData);
                }

                function setHttpAuthHeader() {
                    $http.defaults.headers.common.Authorization = 'Bearer ' + me.userData.bearerToken;
                }

                function retrieveSavedData() {
                    var savedData = localStorageService.get('auth_data');

                    if (typeof savedData === 'undefined' || savedData == null) {
                        throw 'No authentication data exists';
                    } else if (isAuthenticationExpired(savedData.expirationDate)) {
                        throw 'Authentication token has already expired';
                    } else {
                        me.userData = savedData;
                        setHttpAuthHeader();
                    }
                }

                function clearUserData() {
                    me.userData.isAuthenticated = false;
                    me.userData.username = '';
                    me.userData.bearerToken = '';
                    me.userData.expirationDate = null;
                }

                me.userData = {
                    isAuthenticated: false,
                    username: '',
                    bearerToken: '',
                    expirationDate: null
                };

                me.cache = $cacheFactory('tubularHttpCache');
                me.useCache = true;
                me.requireAuthentication = true;
                me.tokenUrl = '/api/token';
                me.refreshTokenUrl = '/api/token';
                me.setTokenUrl = function (val) {
                    me.tokenUrl = val;
                };

                me.isAuthenticated = function () {
                    if (!me.userData.isAuthenticated || isAuthenticationExpired(me.userData.expirationDate)) {
                        try {
                            retrieveSavedData();
                        } catch (e) {
                            return false;
                        }
                    }

                    return true;
                };

                me.setRequireAuthentication = function (val) {
                    me.requireAuthentication = val;
                };

                me.removeAuthentication = function () {
                    removeData();
                    clearUserData();
                    $http.defaults.headers.common.Authorization = null;
                };

                me.authenticate = function (username, password, successCallback, errorCallback, persistData, userDataCallback) {
                    this.removeAuthentication();

                    $http({
                        method: 'POST',
                        url: me.tokenUrl,
                        headers: {
                            'Content-Type': 'application/x-www-form-urlencoded'
                        },
                        data: 'grant_type=password&username=' + username + '&password=' + password
                    }).success(function (data) {
                        me.handleSuccessCallback(userDataCallback, successCallback, persistData, data);
                    }).error(function (data) {
                            me.handleErrorCallback(errorCallback, data);
                        });
                };

                me.handleSuccessCallback = function(userDataCallback, successCallback, persistData, data) {
                    me.userData.isAuthenticated = true;
                    me.userData.username = data.userName || username;
                    me.userData.bearerToken = data.access_token;
                    me.userData.expirationDate = new Date();
                    me.userData.expirationDate = new Date(me.userData.expirationDate.getTime() + data.expires_in * 1000);
                    me.userData.role = data.role;
                    me.userData.refreshToken = data.refresh_token;

                    if (typeof userDataCallback === 'function') {
                        userDataCallback(data);
                    }

                    setHttpAuthHeader();

                    if (persistData) {
                        saveData();
                    }

                    if (typeof successCallback === 'function') {
                        successCallback();
                    }
                };

                me.handleErrorCallback = function(errorCallback, data) {
                    if (typeof errorCallback === 'function') {
                        if (data.error_description) {
                            errorCallback(data.error_description);
                        } else {
                            errorCallback($filter('translate')('UI_HTTPERROR'));
                        }
                    }
                };

                me.addTimeZoneToUrl = function (url) {
                    var separator = url.indexOf('?') === -1 ? '?' : '&';
                    return url + separator + 'timezoneOffset=' + new Date().getTimezoneOffset();
                }

                me.saveDataAsync = function (model, request) {
                    var component = model.$component;
                    model.$component = null;
                    var clone = angular.copy(model);
                    model.$component = component;

                    var originalClone = angular.copy(model.$original);

                    delete clone.$isEditing;
                    delete clone.$hasChanges;
                    delete clone.$selected;
                    delete clone.$original;
                    delete clone.$state;
                    delete clone.$valid;
                    delete clone.$component;
                    delete clone.$isLoading;
                    delete clone.$isNew;

                    if (model.$isNew) {
                        request.serverUrl = me.addTimeZoneToUrl(request.serverUrl);
                        request.data = clone;
                    } else {
                        request.data = {
                            Old: originalClone,
                            New: clone,
                            TimezoneOffset: new Date().getTimezoneOffset()
                        };
                    }

                    var dataRequest = me.retrieveDataAsync(request);

                    dataRequest.promise.then(function (data) {
                        model.$hasChanges = false;
                        model.resetOriginal();

                        return data;
                    });

                    return dataRequest;
                };

                me.getExpirationDate = function () {
                    var date = new Date();
                    var minutes = 5;
                    return new Date(date.getTime() + minutes * 60000);
                };

                me.refreshSession = function(persistData, errorCallback) {
                    $http({
                        method: 'POST',
                        url: me.refreshTokenUrl,
                        headers: {
                            'Content-Type': 'application/x-www-form-urlencoded'
                        },
                        data: 'grant_type=refresh_token&refresh_token=' + me.userData.refreshToken
                    }).success(function(data) {
                        me.handleSuccessCallback(null, null, persistData, data);
                    }).error(function(data) {
                        me.handleErrorCallback(errorCallback, data);
                    });
                };

                me.checksum = function (obj) {
                    var keys = Object.keys(obj).sort();
                    var output = [], prop;

                    for (var i = 0; i < keys.length; i++) {
                        prop = keys[i];
                        output.push(prop);
                        output.push(obj[prop]);
                    }

                    return JSON.stringify(output);
                };

                me.retrieveDataAsync = function (request) {
                    var canceller = $q.defer();

                    var cancel = function (reason) {
                        console.error(reason);
                        canceller.resolve(reason);
                    };

                    if (angular.isUndefined(request.requireAuthentication)) {
                        request.requireAuthentication = me.requireAuthentication;
                    }

                    if (angular.isString(request.requireAuthentication)) {
                        request.requireAuthentication = request.requireAuthentication == "true";
                    }

                    if (request.requireAuthentication && me.isAuthenticated() === false) {
                        if (me.userData.refreshToken) {
                            me.refreshSession(true);
                        } else {
                            return {
                                promise: $q(function (resolve) {
                                    resolve(null);
                                }),
                                cancel: cancel
                            };
                        }
                    }

                    var checksum = me.checksum(request);

                    if ((request.requestMethod == 'GET' || request.requestMethod == 'POST') && me.useCache) {
                        var data = me.cache.get(checksum);

                        if (angular.isDefined(data) && data.Expiration.getTime() > new Date().getTime()) {
                            return {
                                promise: $q(function (resolve) {
                                    resolve(data.Set);
                                }),
                                cancel: cancel
                            };
                        }
                    }

                    request.timeout = request.timeout || 15000;

                    var timeoutHanlder = $timeout(function () {
                        cancel('Timed out');
                    }, request.timeout);

                    var promise = $http({
                        url: request.serverUrl,
                        method: request.requestMethod,
                        data: request.data,
                        timeout: canceller.promise
                    }).then(function (response) {
                        $timeout.cancel(timeoutHanlder);

                        if (me.useCache) {
                            me.cache.put(checksum, { Expiration: me.getExpirationDate(), Set: response.data });
                        }

                        return response.data;
                    }, function (error) {
                        if (angular.isDefined(error) && angular.isDefined(error.status) && error.status == 401) {
                            if (me.isAuthenticated()) {
                                if (me.userData.refreshToken) {
                                    me.refreshSession(true);

                                    return me.retrieveDataAsync(request);
                                } else {
                                    me.removeAuthentication();
                                    // Let's trigger a refresh
                                    document.location = document.location;
                                }
                            }
                        }

                        return $q.reject(error);
                    });

                    return {
                        promise: promise,
                        cancel: cancel
                    };
                };

                me.get = function (url, params) {
                    if (me.requireAuthentication && !me.isAuthenticated()) {
                        var canceller = $q.defer();

                        // Return empty dataset
                        return {
                            promise: $q(function (resolve) {
                                resolve(null);
                            }),
                            cancel: function (reason) {
                                console.error(reason);
                                canceller.resolve(reason);
                            }
                        };
                    }

                    return { promise: $http.get(url, params).then(function (data) { return data.data; }) };
                };

                me.getBinary = function (url) {
                    return me.get(url, { responseType: 'arraybuffer' });
                };

                me.delete = function (url) {
                    return me.retrieveDataAsync({
                        serverUrl: url,
                        requestMethod: 'DELETE'
                    });
                };

                me.post = function (url, data) {
                    return me.retrieveDataAsync({
                        serverUrl: url,
                        requestMethod: 'POST',
                        data: data
                    });
                };

                /*
                 * @function postBinary
                 * 
                 * @description
                 * Allow to post a `FormData` object with `$http`. You need to append the files
                 * in your own FormData.
                 */
                me.postBinary = function (url, formData) {
                    var canceller = $q.defer();

                    var cancel = function (reason) {
                        console.error(reason);
                        canceller.resolve(reason);
                    };

                    if (me.requireAuthentication && me.isAuthenticated() === false) {
                        // Return empty dataset
                        return {
                            promise: $q(function (resolve) {
                                resolve(null);
                            }),
                            cancel: cancel
                        };
                    }

                    var promise = $http({
                        url: url,
                        method: "POST",
                        headers: { 'Content-Type': undefined },
                        transformRequest: function (data) {
                            // TODO: Remove?
                            return data;
                        },
                        data: formData
                    });

                    return {
                        promise: promise,
                        cancel: cancel
                    };
                };

                me.put = function (url, data) {
                    return me.retrieveDataAsync({
                        serverUrl: url,
                        requestMethod: 'PUT',
                        data: data
                    });
                };

                me.getByKey = function (url, key) {
                    var urlData = me.addTimeZoneToUrl(url).split('?');
                    var getUrl = urlData[0] + key;

                    if (urlData.length > 1) getUrl += '?' + urlData[1];

                    return me.get(getUrl);
                };

                // This is a kind of factory to retrieve a DataService
                me.instances = [];

                me.registerService = function (name, instance) {
                    me.instances[name] = instance;
                };

                me.getDataService = function (name) {
                    if (angular.isUndefined(name) || name == null || name === 'tubularHttp') {
                        return me;
                    }

                    var instance = me.instances[name];

                    return instance == null ? me : instance;
                };
            }
        ]);
})(window.angular);
(function() {
    'use strict';

    angular.module('tubular.services')
        /**
         * @ngdoc service
         * @name tubularLocalData
         *
         * @description
         * Use `tubularLocalData` to connect a grid or a form to a local
         * JSON database.
         */
        .service('tubularLocalData', [
            'tubularHttp', '$q', '$filter', function tubularLocalData(tubularHttp, $q, $filter) {
                var me = this;

                me.retrieveDataAsync = function(request) {
                    request.requireAuthentication = false;

                    var cancelFunc = function(reason) {
                        console.error(reason);
                        $q.defer().resolve(reason);
                    };

                    if (request.serverUrl.indexOf('data:') === 0) {
                        return {
                            promise: $q(function (resolve) {
                                var urlData = request.serverUrl.substr('data:application/json;base64,'.length);
                                urlData = atob(urlData);
                                var data = angular.fromJson(urlData);
                                resolve(me.pageRequest(request.data, data));
                            }),
                            cancel: cancelFunc
                        };
                    }

                    // If database is null, retrieve it
                    return {
                        promise: $q(function(resolve, reject) {
                            resolve(tubularHttp.retrieveDataAsync(request).promise.then(function(data) {
                                // TODO: Maybe check dataset and convert DATES
                                return me.pageRequest(request.data, data);
                            }));
                        }),
                        cancel: cancelFunc
                    };
                };

                var reduceFilterArray = function(filters) {
                    var filtersPattern = {};

                    for (var i in filters) {
                        if (filters.hasOwnProperty(i)) {
                            for (var k in filters[i]) {
                                if (filters[i].hasOwnProperty(k)) {
                                    filtersPattern[k] = filters[i][k].toLocaleLowerCase();
                                }
                            }
                        }
                    }

                    return filtersPattern;
                };

                me.pageRequest = function(request, database) {
                    var response = {
                        Counter: 0,
                        CurrentPage: 1,
                        FilteredRecordCount: 0,
                        TotalRecordCount: 0,
                        Payload: [],
                        TotalPages: 0
                    };

                    if (database.length === 0) return response;

                    var set = database;

                    // Get columns with sort
                    // TODO: Check SortOrder 
                    var sorts = request.Columns
                        .filter(function(el) { return el.SortOrder > 0; })
                        .map(function(el) { return (el.SortDirection === 'Descending' ? '-' : '') + el.Name; });

                    for (var sort in sorts) {
                        if (sorts.hasOwnProperty(sort)) {
                            set = $filter('orderBy')(set, sorts[sort]);
                        }
                    }

                    // Get filters (only Contains)
                    // TODO: Implement all operators
                    var filters = request.Columns
                        .filter(function(el) { return el.Filter && el.Filter.Text; })
                        .map(function(el) {
                            var obj = {};
                            if (el.Filter.Operator === 'Contains') {
                                obj[el.Name] = el.Filter.Text;
                            }

                            return obj;
                        });

                    if (filters.length > 0) {
                        set = $filter('filter')(set, reduceFilterArray(filters));
                    }

                    if (request.Search && request.Search.Operator === 'Auto' && request.Search.Text) {
                        var searchables = request.Columns
                            .filter(function(el) { return el.Searchable; })
                            .map(function(el) {
                                var obj = {};
                                obj[el.Name] = request.Search.Text;
                                return obj;
                            });

                        if (searchables.length > 0) {
                            set = $filter('filter')(set, function(value, index, array) {
                                var filters = reduceFilterArray(searchables);
                                var result = false;
                                angular.forEach(filters, function(filter, column) {
                                    if (value[column] && value[column].toLocaleLowerCase().indexOf(filter) >= 0) {
                                        result = true;
                                    }
                                });

                                return result;
                            });
                        }
                    }

                    response.FilteredRecordCount = set.length;
                    response.TotalRecordCount = set.length;
                    response.Payload = set.slice(request.Skip, request.Take + request.Skip);
                    response.TotalPages = (response.FilteredRecordCount + request.Take - 1) / request.Take;

                    if (response.TotalPages > 0) {
                        var shift = Math.pow(10, 0);
                        var number = 1 + ((request.Skip / response.FilteredRecordCount) * response.TotalPages);

                        response.CurrentPage = ((number * shift) | 0) / shift;
                        if (response.CurrentPage < 1) response.CurrentPage = 1;
                    }

                    return response;
                };

                me.saveDataAsync = function(model, request) {
                    // TODO: Complete
                };

                me.get = function(url) {
                    // Just pass the URL to TubularHttp for now
                    tubularHttp.get(url);
                };

                me.delete = function(url) {
                    // Just pass the URL to TubularHttp for now
                    tubularHttp.delete(url);
                };

                me.post = function(url, data) {
                    // Just pass the URL to TubularHttp for now
                    tubularHttp.post(url, data);
                };

                me.put = function(url, data) {
                    // Just pass the URL to TubularHttp for now
                    tubularHttp.put(url, data);
                };

                me.getByKey = function(url, key) {
                    // Just pass the URL to TubularHttp for now
                    tubularHttp.getByKey(url, key);
                };
            }
        ]);
})();
(function() {
    'use strict';

    angular.module('tubular.services')
        /**
         * @ngdoc service
         * @name tubularOData
         *
         * @description
         * Use `tubularOData` to connect a grid or a form to an OData Resource. Most filters are working
         * and sorting and pagination too.
         * 
         * This service provides authentication using bearer-tokens, if you require any other you need to provide it.
         */
        .service('tubularOData', [
            'tubularHttp', function tubularOData(tubularHttp) {
                var me = this;

                // {0} represents column name and {1} represents filter value
                me.operatorsMapping = {
                    'None': '',
                    'Equals': "{0} eq {1}",
                    'NotEquals': "{0} ne {1}",
                    'Contains': "substringof({1}, {0}) eq true",
                    'StartsWith': "startswith({0}, {1}) eq true",
                    'EndsWith': "endswith({0}, {1}) eq true",
                    'NotContains': "substringof({1}, {0}) eq false",
                    'NotStartsWith': "startswith({0}, {1}) eq false",
                    'NotEndsWith': "endswith({0}, {1}) eq false",
                    // TODO: 'Between': 'Between', 
                    'Gte': "{0} ge {1}",
                    'Gt': "{0} gt {1}",
                    'Lte': "{0} le {1}",
                    'Lt': "{0} lt {1}"
                };

                me.generateUrl = function (request) {
                    var params = request.data;

                    var url = request.serverUrl;
                    url += url.indexOf('?') > 0 ? '&' : '?';
                    url += '$format=json&$inlinecount=allpages';

                    url += "&$select=" + params.Columns.map(function (el) { return el.Name; }).join(',');

                    if (params.Take != -1) {
                        url += "&$skip=" + params.Skip;
                        url += "&$top=" + params.Take;
                    }

                    var order = params.Columns
                        .filter(function (el) { return el.SortOrder > 0; })
                        .sort(function (a, b) { return a.SortOrder - b.SortOrder; })
                        .map(function (el) { return el.Name + " " + (el.SortDirection === "Descending" ? "desc" : ""); });

                    if (order.length > 0) {
                        url += "&$orderby=" + order.join(',');
                    }

                    var filter = params.Columns
                        .filter(function (el) { return el.Filter && el.Filter.Text; })
                        .map(function (el) {
                            return me.operatorsMapping[el.Filter.Operator]
                                .replace('{0}', el.Name)
                                .replace('{1}', el.DataType == "string" ? "'" + el.Filter.Text + "'" : el.Filter.Text);
                        })
                        .filter(function (el) { return el.length > 1; });


                    if (params.Search && params.Search.Operator === 'Auto') {
                        var freetext = params.Columns
                            .filter(function (el) { return el.Searchable; })
                            .map(function (el) {
                                return "startswith({0}, '{1}') eq true".replace('{0}', el.Name).replace('{1}', params.Search.Text);
                            });

                        if (freetext.length > 0) {
                            filter.push("(" + freetext.join(' or ') + ")");
                        }
                    }

                    if (filter.length > 0) {
                        url += "&$filter=" + filter.join(' and ');
                    }

                    return url;
                };

                me.retrieveDataAsync = function (request) {
                    var params = request.data;
                    var originalUrl = request.serverUrl;
                    request.serverUrl = me.generateUrl(request);
                    request.data = null;

                    var response = tubularHttp.retrieveDataAsync(request);

                    var promise = response.promise.then(function (data) {
                        var result = {
                            Payload: data.value,
                            CurrentPage: 1,
                            TotalPages: 1,
                            TotalRecordCount: 1,
                            FilteredRecordCount: 1
                        };

                        result.TotalRecordCount = parseInt(data["odata.count"]);
                        result.FilteredRecordCount = result.TotalRecordCount; // TODO: Calculate filtered items
                        result.TotalPages = parseInt((result.FilteredRecordCount + params.Take - 1) / params.Take);
                        result.CurrentPage = parseInt(1 + ((params.Skip / result.FilteredRecordCount) * result.TotalPages));

                        if (result.CurrentPage > result.TotalPages) {
                            result.CurrentPage = 1;
                            request.data = params;
                            request.data.Skip = 0;

                            request.serverUrl = originalUrl;

                            me.retrieveDataAsync(request).promise.then(function (newData) {
                                result.Payload = newData.value;
                            });
                        }

                        return result;
                    });

                    return {
                        promise: promise,
                        cancel: response.cancel
                    };
                };

                me.saveDataAsync = function (model, request) {
                    return tubularHttp.saveDataAsync(model, request); //TODO: Check how to handle
                };

                me.get = function (url) {
                    return tubularHttp.get(url);
                };

                me.delete = function (url) {
                    return tubularHttp.delete(url);
                };

                me.post = function (url, data) {
                    return tubularHttp.post(url, data);
                };

                me.put = function (url, data) {
                    return tubularHttp.put(url, data);
                };

                me.getByKey = function (url, key) {
                    return tubularHttp.get(url + "(" + key + ")");
                };
            }
        ]);
})();
(function (angular) {
    'use strict';

    angular.module('tubular.services')
        /**
         * @ngdoc service
         * @name tubularTemplateService
         *
         * @description
         * Use `tubularTemplateService` to generate `tbGrid` and `tbForm` templates.
         * 
         * This service is just a facade to the node module expose like `tubularTemplateServiceModule`.
         */
        .service('tubularTemplateService', ['$templateCache', 'tubularEditorService',
            function tubularTemplateService($templateCache, tubularEditorService) {
                var me = this;

                me.enums = tubularTemplateServiceModule.enums;
                me.defaults = tubularTemplateServiceModule.defaults;

                // Loading popovers templates
                me.tbColumnFilterPopoverTemplateName = 'tbColumnFilterPopoverTemplate.html';
                me.tbColumnDateTimeFilterPopoverTemplateName = 'tbColumnDateTimeFilterPopoverTemplate.html';
                me.tbColumnOptionsFilterPopoverTemplateName = 'tbColumnOptionsFilterPopoverTemplate.html';
                me.tbRemoveButtonrPopoverTemplateName = 'tbRemoveButtonrPopoverTemplate.html';

                if (!$templateCache.get(me.tbColumnFilterPopoverTemplateName)) {
                    me.tbColumnFilterPopoverTemplate = '<div>' +
                        '<form class="tubular-column-filter-form" onsubmit="return false;">' +
                        '<select class="form-control" ng-options="key as value for (key , value) in $ctrl.filterOperators" ng-model="$ctrl.filter.Operator" ng-hide="$ctrl.dataType == \'boolean\'"></select>&nbsp;' +
                        '<input class="form-control" type="search" ng-model="$ctrl.filter.Text" autofocus ng-keypress="$ctrl.checkEvent($event)" ng-hide="$ctrl.dataType == \'boolean\'"' +
                        'placeholder="{{\'CAPTION_VALUE\' | translate}}" ng-disabled="$ctrl.filter.Operator == \'None\'" />' +
                        '<div class="text-center" ng-show="$ctrl.dataType == \'boolean\'">' +
                        '<button type="button" class="btn btn-default btn-md" ng-disabled="$ctrl.filter.Text === true" ng-click="$ctrl.filter.Text = true; $ctrl.filter.Operator = \'Equals\';">' +
                        '<i class="fa fa-check"></i></button>&nbsp;' +
                        '<button type="button" class="btn btn-default btn-md" ng-disabled="$ctrl.filter.Text === false" ng-click="$ctrl.filter.Text = false; $ctrl.filter.Operator = \'Equals\';">' +
                        '<i class="fa fa-times"></i></button></div>' +
                        '<input type="search" class="form-control" ng-model="$ctrl.filter.Argument[0]" ng-keypress="$ctrl.checkEvent($event)" ng-show="$ctrl.filter.Operator == \'Between\'" />' +
                        '<hr />' +
                        '<tb-column-filter-buttons></tb-column-filter-buttons>' +
                        '</form></div>';

                    $templateCache.put(me.tbColumnFilterPopoverTemplateName, me.tbColumnFilterPopoverTemplate);
                }

                if ($templateCache.get(me.tbColumnDateTimeFilterPopoverTemplateName)) {
                    me.tbColumnDateTimeFilterPopoverTemplate = '<div>' +
                        '<form class="tubular-column-filter-form" onsubmit="return false;">' +
                        '<select class="form-control" ng-options="key as value for (key , value) in $ctrl.filterOperators" ng-model="$ctrl.filter.Operator" ng-hide="$ctrl.dataType == \'boolean\'"></select>&nbsp;' +


                        (tubularEditorService.canUseHtml5Date ?
                            '<input class="form-control" type="date" ng-model="$ctrl.filter.Text" autofocus ng-keypress="$ctrl.checkEvent($event)" '+
                            'placeholder="{{\'CAPTION_VALUE\' | translate}}" ng-disabled="$ctrl.filter.Operator == \'None\'" />' +
                            '<input type="date" class="form-control" ng-model="$ctrl.filter.Argument[0]" ng-keypress="$ctrl.checkEvent($event)" ng-show="$ctrl.filter.Operator == \'Between\'" />'
                            :
                            '<div class="input-group">' +
                            '<input type="text" class="form-control" uib-datepicker-popup="MM/dd/yyyy" ng-model="$ctrl.filter.Text" autofocus ng-keypress="$ctrl.checkEvent($event)" ' +
                            'placeholder="{{\'CAPTION_VALUE\' | translate}}" ng-disabled="$ctrl.filter.Operator == \'None\'" is-open="$ctrl.dateOpen" />' +
                            '<span class="input-group-btn">' +
                            '<button type="button" class="btn btn-default" ng-click="$ctrl.dateOpen = !$ctrl.dateOpen;"><i class="fa fa-calendar"></i></button>' +
                            '</span>' +
                            '</div>'
                            ) +
                        '<hr />' +
                        '<tb-column-filter-buttons></tb-column-filter-buttons>' +
                        '</form></div>';

                    $templateCache.put(me.tbColumnDateTimeFilterPopoverTemplateName, me.tbColumnDateTimeFilterPopoverTemplate);
                }

                if (!$templateCache.get(me.tbColumnOptionsFilterPopoverTemplateName)) {
                    me.tbColumnOptionsFilterPopoverTemplate = '<div>' +
                        '<form class="tubular-column-filter-form" onsubmit="return false;">' +
                        '<select class="form-control checkbox-list" ng-options="item for item in $ctrl.optionsItems" ' +
                        'ng-model="$ctrl.filter.Argument" multiple ng-disabled="$ctrl.dataIsLoaded == false"></select>&nbsp;' +
                        '<hr />' +
                        '<tb-column-filter-buttons></tb-column-filter-buttons>' +
                        '</form></div>';

                    $templateCache.put(me.tbColumnOptionsFilterPopoverTemplateName, me.tbColumnOptionsFilterPopoverTemplate);
                }

                if (!$templateCache.get(me.tbRemoveButtonrPopoverTemplateName)) {
                    me.tbRemoveButtonrPopoverTemplate = '<div class="tubular-remove-popover">' +
                        '<button ng-click="$ctrl.model.delete()" class="btn btn-danger btn-xs">' +
                        '{{:: $ctrl.caption || (\'CAPTION_REMOVE\' | translate) }}' +
                        '</button>' +
                        '&nbsp;' +
                        '<button ng-click="$ctrl.isOpen = false;" class="btn btn-default btn-xs">' +
                        '{{:: $ctrl.cancelCaption || (\'CAPTION_CANCEL\' | translate) }}' +
                        '</button>' +
                        '</div>';

                    $templateCache.put(me.tbRemoveButtonrPopoverTemplateName, me.tbRemoveButtonrPopoverTemplate);
                }

                me.generatePopup = function(model, title) {
                    var templateName = 'temp' + (new Date().getTime()) + '.html';
                    var template = tubularTemplateServiceModule.generatePopup(model, title);

                    $templateCache.put(templateName, template);

                    return templateName;
                };

                me.createColumns = function(model) {
                    return tubularTemplateServiceModule.createColumns(model);
                };

                me.generateForm = function(fields, options) {
                    return tubularTemplateServiceModule.generateForm(fields, options);
                };

                me.generateGrid = function(columns, options) {
                    return tubularTemplateServiceModule.generateGrid(columns, options);
                };
            }
        ]);
})(window.angular);
(function (angular) {
    'use strict';

    angular.module('tubular.services')
        /**
         * @ngdoc service
         * @name tubularTranslate
         *
         * @description
         * Use `tubularTranslate` to translate strings.
         */
        .service('tubularTranslate', [
            function tubularTranslate() {
                var me = this;

                me.currentLanguage = 'en';
                me.defaultLanguage = 'en';

                me.translationTable = {
                    'en': {
                        'EDITOR_REGEX_DOESNT_MATCH': "The field doesn't match the regular expression.",
                        'EDITOR_REQUIRED': "The field is required.",
                        'EDITOR_MIN_CHARS': "The field needs to be minimum {0} chars.",
                        'EDITOR_MAX_CHARS': "The field needs to be maximum {0} chars.",
                        'EDITOR_MIN_NUMBER': "The minimum number is {0}.",
                        'EDITOR_MAX_NUMBER': "The maximum number is {0}.",
                        'EDITOR_MIN_DATE': "The minimum date is {0}.",
                        'EDITOR_MAX_DATE': "The maximum date is {0}.",
                        'EDITOR_MATCH': 'The field needs to match the {0} field.',
                        'CAPTION_APPLY': 'Apply',
                        'CAPTION_CLEAR': 'Clear',
                        'CAPTION_CLOSE': 'Close',
                        'CAPTION_SELECTCOLUMNS': 'Select Columns',
                        'CAPTION_FILTER': 'Filter',
                        'CAPTION_VALUE': 'Value',
                        'CAPTION_REMOVE': 'Remove',
                        'CAPTION_CANCEL': 'Cancel',
                        'CAPTION_EDIT': 'Edit',
                        'CAPTION_SAVE': 'Save',
                        'CAPTION_PRINT': 'Print',
                        'CAPTION_LOAD': 'Load',
                        'CAPTION_ADD': 'Add',
                        'UI_SEARCH': 'search . . .',
                        'UI_PAGESIZE': 'Page size:',
                        'UI_EXPORTCSV': 'Export CSV',
                        'UI_CURRENTROWS': 'Current rows',
                        'UI_ALLROWS': 'All rows',
                        'UI_REMOVEROW': 'Do you want to delete this row?',
                        'UI_SHOWINGRECORDS': 'Showing {0} to {1} of {2} records',
                        'UI_FILTEREDRECORDS': '(Filtered from {0} total records)',
                        'UI_HTTPERROR': 'Unable to contact server; please, try again later.',
                        'UI_GENERATEREPORT': 'Generate Report',
                        'UI_TWOCOLS': 'Two columns',
                        'UI_ONECOL': 'One column',
                        'UI_MAXIMIZE': 'Maximize',
                        'UI_RESTORE': 'Restore',
                        'UI_MOVEUP': 'Move Up',
                        'UI_MOVEDOWN': 'Move Down',
                        'UI_MOVELEFT': 'Move Left',
                        'UI_MOVERIGHT': 'Move Right',
                        'UI_COLLAPSE': 'Collapse',
                        'UI_EXPAND': 'Expand',
                        'OP_NONE': 'None',
                        'OP_EQUALS': 'Equals',
                        'OP_NOTEQUALS': 'Not Equals',
                        'OP_CONTAINS': 'Contains',
                        'OP_NOTCONTAINS': 'Not Contains',
                        'OP_STARTSWITH': 'Starts With',
                        'OP_NOTSTARTSWITH': 'Not Starts With',
                        'OP_ENDSWITH': 'Ends With',
                        'OP_NOTENDSWITH': 'Not Ends With',
                        'OP_BETWEEN': 'Between'
                    },
                    'es': {
                        'EDITOR_REGEX_DOESNT_MATCH': "El campo no es válido contra la expresión regular.",
                        'EDITOR_REQUIRED': "El campo es requerido.",
                        'EDITOR_MIN_CHARS': "El campo requiere mínimo {0} caracteres.",
                        'EDITOR_MAX_CHARS': "El campo requiere máximo {0} caracteres.",
                        'EDITOR_MIN_NUMBER': "El número mínimo es {0}.",
                        'EDITOR_MAX_NUMBER': "El número maximo es {0}.",
                        'EDITOR_MIN_DATE': "La fecha mínima es {0}.",
                        'EDITOR_MAX_DATE': "La fecha máxima es {0}.",
                        'EDITOR_MATCH': 'El campo debe de conincidir con el campo {0}.',
                        'CAPTION_APPLY': 'Aplicar',
                        'CAPTION_CLEAR': 'Limpiar',
                        'CAPTION_CLOSE': 'Cerrar',
                        'CAPTION_SELECTCOLUMNS': 'Seleccionar columnas',
                        'CAPTION_FILTER': 'Filtro',
                        'CAPTION_VALUE': 'Valor',
                        'CAPTION_REMOVE': 'Remover',
                        'CAPTION_CANCEL': 'Cancelar',
                        'CAPTION_EDIT': 'Editar',
                        'CAPTION_SAVE': 'Guardar',
                        'CAPTION_PRINT': 'Imprimir',
                        'CAPTION_LOAD': 'Cargar',
                        'CAPTION_ADD': 'Agregar',
                        'UI_SEARCH': 'buscar . . .',
                        'UI_PAGESIZE': '# Registros:',
                        'UI_EXPORTCSV': 'Exportar CSV',
                        'UI_CURRENTROWS': 'Esta página',
                        'UI_ALLROWS': 'Todo',
                        'UI_REMOVEROW': '¿Desea eliminar el registro?',
                        'UI_SHOWINGRECORDS': 'Mostrando registros {0} al {1} de {2}',
                        'UI_FILTEREDRECORDS': '(De un total de {0} registros)',
                        'UI_HTTPERROR': 'No se logro contactar el servidor, intente más tarde.',
                        'UI_GENERATEREPORT': 'Generar Reporte',
                        'UI_TWOCOLS': 'Dos columnas',
                        'UI_ONECOL': 'Una columna',
                        'UI_MAXIMIZE': 'Maximizar',
                        'UI_RESTORE': 'Restaurar',
                        'UI_MOVEUP': 'Mover Arriba',
                        'UI_MOVEDOWN': 'Mover Abajo',
                        'UI_MOVELEFT': 'Mover Izquierda',
                        'UI_MOVERIGHT': 'Mover Derecha',
                        'UI_COLLAPSE': 'Colapsar',
                        'UI_EXPAND': 'Expandir',
                        'OP_NONE': 'Ninguno',
                        'OP_EQUALS': 'Igual',
                        'OP_NOTEQUALS': 'No Igual',
                        'OP_CONTAINS': 'Contiene',
                        'OP_NOTCONTAINS': 'No Contiene',
                        'OP_STARTSWITH': 'Comienza Con',
                        'OP_NOTSTARTSWITH': 'No Comienza Con',
                        'OP_ENDSWITH': 'Termina Con',
                        'OP_NOTENDSWITH': 'No Termina Con',
                        'OP_BETWEEN': 'Entre'
                    }
                };

                me.setLanguage = function(language) {
                    // TODO: Check translationTable first
                    me.currentLanguage = language;

                    return me;
                };

                me.addTranslation = function(language, key, value) {
                    var languageTable = me.translationTable[language] || me.translationTable[me.currentLanguage] || me.translationTable[me.defaultLanguage];
                    languageTable[key] = value;

                    return me;
                }

                me.translate = function(key) {
                    var languageTable = me.translationTable[me.currentLanguage] || me.translationTable[me.defaultLanguage];

                    return languageTable[key] || key;
                };

                me.reverseTranslate = function(value) {
                    // TODO: Find value
                };
            }
        ])
        /**
         * @ngdoc filter
         * @name translate
         *
         * @description
         * Translate a key to the current language
         */
        .filter('translate', [
            'tubularTranslate', function(tubularTranslate) {
                return function(input, param1, param2, param3, param4) {
                    // TODO: Probably send an optional param to define language
                    if (angular.isDefined(input)) {
                        var translation = tubularTranslate.translate(input);

                        translation = translation.replace("{0}", param1 || '');
                        translation = translation.replace("{1}", param2 || '');
                        translation = translation.replace("{2}", param3 || '');
                        translation = translation.replace("{3}", param4 || '');

                        return translation;
                    }

                    return input;
                };
            }
        ]);
})(window.angular);