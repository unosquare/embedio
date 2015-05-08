///#source 1 1 tubular/node-module.js
'use strict';

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
        sortDirections: ['Ascending','Descending']
    },

    defaults: {
        gridOptions: {
            Pager: true,
            FreeTextSearch: true,
            PageSizeSelector: true,
            PagerInfo: true,
            ExportCsv: true,
            Mode: 'Read-Only',
            RequireAuthentication: false
        },
        formOptions: {
            CancelButton: true,
            SaveUrl: '',
            SaveMethod: 'POST',
            Layout: 'Simple',
            ModelKey: '',
            RequireAuthentication: false
        }
    },

    isNumber: function(value) { return typeof value === 'number'; },

    isDate: function(value) {
        return toString.call(value) === '[object Date]';
    },

    createColumns: function(model) {
        var jsonModel = (model instanceof Array && model.length > 0) ? model[0] : model;
        var columns = [];

        for (var prop in jsonModel) {
            if (jsonModel.hasOwnProperty(prop)) {
                var value = jsonModel[prop];
                // Ignore functions
                if (prop[0] === '$' || typeof (value) === 'function') continue;
                // Ignore null value, but maybe evaluate another item if there is anymore
                if (value == null) continue;

                if (this.isNumber(value) || parseFloat(value).toString() == value) {
                    columns.push({ Name: prop, DataType: 'numeric', Template: '{{row.' + prop + '}}' });
                } else if (this.isDate(value) || isNaN((new Date(value)).getTime()) == false) {
                    columns.push({ Name: prop, DataType: 'date', Template: '{{row.' + prop + ' | date}}' });
                } else if (value.toLowerCase() == 'true' || value.toLowerCase() == 'false') {
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
                columnObj.SortDirection = 'Ascending';
                // Form attributes
                columnObj.ShowLabel = true;
                columnObj.Placeholder = '';
                columnObj.Format = '';
                columnObj.Help = '';
                columnObj.Required = true;
                columnObj.ReadOnly = false;

                if (firstSort === false) {
                    columnObj.IsKey = true;
                    columnObj.SortOrder = 1;
                    firstSort = true;
                }
            }
        }

        return columns;
    },

    generateFieldsArray: function(columns) {
        return columns.map(function(el) {
            var editorTag = el.EditorType.replace(/([A-Z])/g, function($1) { return "-" + $1.toLowerCase(); });

            return '\r\n\t<' + editorTag + ' name="' + el.Name + '" label="' + el.Label + '" editor-type="' + el.DataType + '" ' +
                '\r\n\t\tshow-label="' + el.ShowLabel + '" placeholder="' + el.Placeholder + '" required="' + el.Required + '" ' +
                '\r\n\t\tread-only="' + el.ReadOnly + '" format="' + el.Format + '" help="' + el.Help + '">' +
                '\r\n\t</' + editorTag + '>';
        });
    },

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

    generateForm: function(fields, options) {
        var layout = options.Layout == 'Simple' ? '' : options.Layout.toLowerCase();
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
            'server-url="' + options.dataUrl + '" server-save-url="' + options.SaveUrl + '" ' +
            (options.isOData ? ' service-name="odata"' : '') + '>' +
            '\r\n\t<h1>Autogenerated Form</h1>' +
            fieldsMarkup +
            '\r\n\t<div>' +
            '\r\n\t\t<button class="btn btn-primary" ng-click="$parent.save()" ng-disabled="!$parent.model.$valid()">Save</button>' +
            (options.CancelButton ? '\r\n\t\t<button class="btn btn-danger" ng-click="$parent.cancel()" formnovalidate>Cancel</button>' : '') +
            '\r\n\t</div>' +
            '\r\n</tb-form>';
    },

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

        return '<h1>Autogenerated Grid</h1>' +
            '\r\n<div class="container">' +
            '\r\n<tb-grid server-url="' + options.dataUrl + '" request-method="GET" class="row" ' +
            'page-size="10" require-authentication="' + options.RequireAuthentication + '" ' +
            (options.isOData ? ' service-name="odata"' : '') +
            (options.Mode != 'Read-Only' ? ' editor-mode="' + options.Mode.toLowerCase() + '"' : '') + '>' +
            (topToolbar === '' ? '' : '\r\n\t<div class="row">' + topToolbar + '\r\n\t</div>') +
            '\r\n\t<div class="row">' +
            '\r\n\t<div class="col-md-12">' +
            '\r\n\t<div class="panel panel-default panel-rounded">' +
            '\r\n\t<tb-grid-table class="table-bordered">' +
            '\r\n\t<tb-column-definitions>' +
            (options.Mode != 'Read-Only' ? '\r\n\t\t<tb-column label="Actions"><tb-column-header>{{label}}</tb-column-header></tb-column>' : '') +
            columns.map(function(el) {
                return '\r\n\t\t<tb-column name="' + el.Name + '" label="' + el.Label + '" column-type="' + el.DataType + '" sort-direction="' + el.SortDirection + '" sortable="' + el.Sortable + '" ' +
                    '\r\n\t\t\tsort-order="' + el.SortOrder + '" is-key="' + el.IsKey + '" searchable="' + el.Searchable + '" visible="' + el.Visible + '">' +
                    (el.Filter ? '<tb-column-filter></tb-column-filter>' : '') +
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
            columns.map(function(el) {
                var editorTag = el.EditorType.replace(/([A-Z])/g, function($1) { return "-" + $1.toLowerCase(); });

                return '\r\n\t\t<tb-cell-template column-name="' + el.Name + '">' +
                    (options.Mode == 'Inline' ?
                        '<' + editorTag + ' is-editing="row.$isEditing" value="row.' + el.Name + '"></' + editorTag + '>' :
                        '\r\n\t\t\t' + el.Template) +
                    '\r\n\t\t</tb-cell-template>';
            }).join('') +
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
///#source 1 1 angular-filter/watcher.js
/**
 * @ngdoc provider
 * @name filterWatcher
 * @kind function
 *
 * @description
 * store specific filters result in $$cache, based on scope life time(avoid memory leak).
 * on scope.$destroy remove it's cache from $$cache container
 */

angular.module('a8m.filter-watcher', [])
  .provider('filterWatcher', function () {

      this.$get = ['$window', '$rootScope', function ($window, $rootScope) {

          /**
           * Cache storing
           * @type {Object}
           */
          var $$cache = {};

          /**
           * Scope listeners container
           * scope.$destroy => remove all cache keys
           * bind to current scope.
           * @type {Object}
           */
          var $$listeners = {};

          /**
           * $timeout without triggering the digest cycle
           * @type {function}
           */
          var $$timeout = $window.setTimeout;

          /**
           * @description
           * get `HashKey` string based on the given arguments.
           * @param fName
           * @param args
           * @returns {string}
           */
          function getHashKey(fName, args) {
              return [fName, angular.toJson(args)]
                .join('#')
                .replace(/"/g, '');
          }

          /**
           * @description
           * fir on $scope.$destroy,
           * remove cache based scope from `$$cache`,
           * and remove itself from `$$listeners`
           * @param event
           */
          function removeCache(event) {
              var id = event.targetScope.$id;
              forEach($$listeners[id], function (key) {
                  delete $$cache[key];
              });
              delete $$listeners[id];
          }

          /**
           * @description
           * for angular version that greater than v.1.3.0
           * it clear cache when the digest cycle is end.
           */
          function cleanStateless() {
              $$timeout(function () {
                  if (!$rootScope.$$phase)
                      $$cache = {};
              });
          }

          /**
           * @description
           * Store hashKeys in $$listeners container
           * on scope.$destroy, remove them all(bind an event).
           * @param scope
           * @param hashKey
           * @returns {*}
           */
          function addListener(scope, hashKey) {
              var id = scope.$id;
              if (isUndefined($$listeners[id])) {
                  scope.$on('$destroy', removeCache);
                  $$listeners[id] = [];
              }
              return $$listeners[id].push(hashKey);
          }

          /**
           * @description
           * return the `cacheKey` or undefined.
           * @param filterName
           * @param args
           * @returns {*}
           */
          function $$isMemoized(filterName, args) {
              var hashKey = getHashKey(filterName, args);
              return $$cache[hashKey];
          }

          /**
         * @description
         * Test if given object is a Scope instance
         * @param obj
         * @returns {Boolean}
         */
          function isScope(obj) {
              return obj && obj.$evalAsync && obj.$watch;
          }

          /**
           * @description
           * store `result` in `$$cache` container, based on the hashKey.
           * add $destroy listener and return result
           * @param filterName
           * @param args
           * @param scope
           * @param result
           * @returns {*}
           */
          function $$memoize(filterName, args, scope, result) {
              var hashKey = getHashKey(filterName, args);
              //store result in `$$cache` container
              $$cache[hashKey] = result;
              // for angular versions that less than 1.3
              // add to `$destroy` listener, a cleaner callback
              if (isScope(scope)) {
                  addListener(scope, hashKey);
              } else {
                  cleanStateless();
              }
              return result;
          }

          return {
              isMemoized: $$isMemoized,
              memoize: $$memoize
          }

      }];
  });
///#source 1 1 angular-filter/group-by.js
/**
 * @ngdoc filter
 * @name groupBy
 * @kind function
 *
 * @description
 * Create an object composed of keys generated from the result of running each element of a collection,
 * each key is an array of the elements.
 */

angular.module('a8m.group-by', ['a8m.filter-watcher'])

  .filter('groupBy', ['$parse', 'filterWatcher', function ($parse, filterWatcher) {
      return function (collection, property) {

          if (!angular.isObject(collection) || angular.isUndefined(property)) {
              return collection;
          }

          var getterFn = $parse(property);

          return filterWatcher.isMemoized('groupBy', arguments) ||
            filterWatcher.memoize('groupBy', arguments, this,
              _groupBy(collection, getterFn));

          /**
           * groupBy function
           * @param collection
           * @param getter
           * @returns {{}}
           */
          function _groupBy(collection, getter) {
              var result = {};
              var prop;

              angular.forEach(collection, function (elm) {
                  prop = getter(elm);

                  if (!result[prop]) {
                      result[prop] = [];
                  }
                  result[prop].push(elm);
              });
              return result;
          }
      }
  }]);
///#source 1 1 tubular/tubular.js
(function() {
    'use strict';

    // TODO: Maybe I need to create a tubular module to move filters and constants

    /**
     * @ngdoc module
     * @name tubular.directives
     * 
     * @description 
     * Tubular Directives module. All the required directives are in this module.
     * 
     * It depends upon {@link tubular.services} and {@link tubular.models}.
     */
    angular.module('tubular.directives', ['tubular.services', 'tubular.models', 'LocalStorageModule','a8m.group-by'])
        .config([
            'localStorageServiceProvider', function (localStorageServiceProvider) {
                localStorageServiceProvider.setPrefix('tubular');

                // define console methods if not defined
                if (typeof console === "undefined") {
                    window.console = {
                        log: function () { },
                        debug: function () { },
                        error: function () { },
                        assert: function () { },
                        info: function () { },
                        warn: function () { },
                    };
                }
            }
        ])
        /**
         * @ngdoc constants
         * @name tubularConst
         *
         * @description
         * The `tubularConst` holds some UI constants.
         */
        .constant("tubularConst", {
            "upCssClass": "fa-long-arrow-up",
            "downCssClass": "fa-long-arrow-down"
        })
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
        .filter('errormessage', function () {
            return function (input) {
                if (angular.isDefined(input) && angular.isDefined(input.data) &&
                    input.data != null &&
                    angular.isDefined(input.data.ExceptionMessage))
                    return input.data.ExceptionMessage;

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
        .filter('numberorcurrency', [
            '$filter', function ($filter) {
                return function (input, format, symbol, fractionSize) {
                    symbol = symbol || "$";
                    fractionSize = fractionSize || 2;

                    if (format === 'C') {
                        return $filter('currency')(input, symbol, fractionSize);
                    }

                    return $filter('number')(input, fractionSize);
                };
            }
        ])
        /**
         * @ngdoc filter
         * @name characters
         * @kind function
         *
         * @description
         * `characters` filter truncates a sentence to a number of characters.
         * 
         * Based on https://github.com/sparkalow/angular-truncate/blob/master/src/truncate.js
         */ 
        .filter('characters', function () {
            return function (input, chars, breakOnWord) {
                if (isNaN(chars)) return input;
                if (chars <= 0) return '';

                if (input && input.length > chars) {
                    input = input.substring(0, chars);

                    if (!breakOnWord) {
                        var lastspace = input.lastIndexOf(' ');

                        //get last space
                        if (lastspace !== -1) {
                            input = input.substr(0, lastspace);
                        }
                    } else {
                        while (input.charAt(input.length - 1) === ' ') {
                            input = input.substr(0, input.length - 1);
                        }
                    }
                    return input + '…';
                }

                return input;
            };
        });
})();
///#source 1 1 tubular/tubular-directives.js
(function() {
    'use strict';

    angular.module('tubular.directives')
        /**
         * @ngdoc directive
         * @name tbGrid
         * @restrict E
         *
         * @description
         * The `tbGrid` directive is the base to create any grid. This is the root node where you should start
         * designing your grid. Don't need to add a `controller`.
         * 
         * @scope
         * 
         * @param {string} serverUrl Set the HTTP URL where the data comes.
         * @param {string} serverSaveUrl Set the HTTP URL where the data will be saved.
         * @param {int} pageSize Define how many records to show in a page, default 20.
         * @param {function} onBeforeGetData Callback to execute before to get data from service.
         * @param {string} requestMethod Set HTTP Method to get data.
         * @param {object} gridDataService Define Data service (instance) to retrieve data, defaults `tubularHttp`.
         * @param {string} gridDataServiceName Define Data service (name) to retrieve data, defaults `tubularHttp`.
         * @param {bool} requireAuthentication Set if authentication check must be executed, default true.
         * @param {string} name Grid's name, used to store metainfo in localstorage.
         * @param {string} editorMode Define if grid is read-only or it has editors (inline or popup).
         * @param {bool} showLoading Set if an overlay will show when it's loading data, default true.
         */
        .directive('tbGrid', [
            function() {
                return {
                    template: '<div class="tubular-grid">' +
                        '<div class="tubular-overlay" ng-show="showLoading && currentRequest != null"><div><div class="fa fa-refresh fa-2x fa-spin"></div></div></div>' +
                        '<ng-transclude></ng-transclude>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: {
                        serverUrl: '@',
                        serverSaveUrl: '@',
                        serverSaveMethod: '@',
                        pageSize: '@?',
                        onBeforeGetData: '=?',
                        requestMethod: '@',
                        gridDataService: '=?service',
                        gridDataServiceName: '@?serviceName',
                        requireAuthentication: '@?',
                        name: '@?gridName',
                        editorMode: '@?',
                        showLoading: '=?'
                    },
                    controller: [
                        '$scope', 'localStorageService', 'tubularPopupService', 'tubularModel', 'tubularHttp', 'tubularOData', '$routeParams',
                        function($scope, localStorageService, tubularPopupService, TubularModel, tubularHttp, tubularOData, $routeParams) {

                            $scope.tubularDirective = 'tubular-grid';
                            $scope.columns = [];
                            $scope.rows = [];
                            $scope.currentPage = 0;
                            $scope.totalPages = 0;
                            $scope.totalRecordCount = 0;
                            $scope.filteredRecordCount = 0;
                            $scope.requestedPage = 1;
                            $scope.hasColumnsDefinitions = false;
                            $scope.requestCounter = 0;
                            $scope.requestMethod = $scope.requestMethod || 'POST';
                            $scope.serverSaveMethod = $scope.serverSaveMethod || 'POST';
                            $scope.requestTimeout = 10000;
                            $scope.currentRequest = null;
                            $scope.autoSearch = $routeParams.param || '';
                            $scope.search = {
                                Text: $scope.autoSearch,
                                Operator: $scope.autoSearch == '' ? 'None' : 'Auto'
                            };
                            $scope.isEmpty = false;
                            $scope.tempRow = new TubularModel($scope, {});
                            $scope.gridDataService = $scope.gridDataService || tubularHttp;
                            $scope.requireAuthentication = $scope.requireAuthentication || true;
                            $scope.name = $scope.name || 'tbgrid';
                            $scope.editorMode = $scope.editorMode || 'none';
                            $scope.canSaveState = false;
                            $scope.groupBy = '';
                            $scope.showLoading = $scope.showLoading || true;

                            // Helper to use OData without controller
                            if ($scope.gridDataServiceName === 'odata') {
                                $scope.gridDataService = tubularOData;
                            }

                            $scope.$watch('columns', function() {
                                if ($scope.hasColumnsDefinitions === false || $scope.canSaveState === false)
                                    return;

                                localStorageService.set($scope.name + "_columns", $scope.columns);
                            }, true);

                            $scope.addColumn = function(item) {
                                if (item.Name === null) return;

                                if ($scope.hasColumnsDefinitions !== false)
                                    throw 'Cannot define more columns. Column definitions have been sealed';

                                $scope.columns.push(item);
                            };

                            $scope.newRow = function(template, popup) {
                                $scope.tempRow = new TubularModel($scope, {}, $scope.gridDataService);
                                $scope.tempRow.$isNew = true;
                                $scope.tempRow.$isEditing = true;

                                if (angular.isDefined(template)) {
                                    if (angular.isDefined(popup) && popup) {
                                        tubularPopupService.openDialog(template, $scope.tempRow);
                                    }
                                }
                            };

                            $scope.deleteRow = function(row) {
                                var request = {
                                    serverUrl: $scope.serverSaveUrl + "/" + row.$key,
                                    requestMethod: 'DELETE',
                                    timeout: $scope.requestTimeout,
                                    requireAuthentication: $scope.requireAuthentication,
                                };

                                $scope.currentRequest = $scope.gridDataService.retrieveDataAsync(request);

                                $scope.currentRequest.promise.then(
                                    function(data) {
                                        row.$hasChanges = false;
                                        $scope.$emit('tbGrid_OnRemove', data);
                                    }, function(error) {
                                        $scope.$emit('tbGrid_OnConnectionError', error);
                                    }).then(function() {
                                    $scope.currentRequest = null;
                                    $scope.retrieveData();
                                });
                            };

                            $scope.verifyColumns = function() {
                                var columns = localStorageService.get($scope.name + "_columns");
                                if (columns === null || columns === "") {
                                    // Nothing in settings, saving initial state
                                    localStorageService.set($scope.name + "_columns", $scope.columns);
                                    return;
                                }

                                for (var index in columns) {
                                    var columnName = columns[index].Name;
                                    var filtered = $scope.columns.filter(function(el) { return el.Name == columnName; });

                                    if (filtered.length == 0) continue;

                                    var current = filtered[0];
                                    // Updates visibility by now
                                    current.Visible = columns[index].Visible;
                                    // TODO: Restore filters
                                }
                            };

                            $scope.retrieveData = function() {
                                $scope.canSaveState = true;
                                $scope.verifyColumns();

                                $scope.pageSize = $scope.pageSize || 20;
                                var page = $scope.requestedPage == 0 ? 1 : $scope.requestedPage;

                                var request = {
                                    serverUrl: $scope.serverUrl,
                                    requestMethod: $scope.requestMethod,
                                    timeout: $scope.requestTimeout,
                                    requireAuthentication: $scope.requireAuthentication,
                                    data: {
                                        Count: $scope.requestCounter,
                                        Columns: $scope.columns,
                                        Skip: (page - 1) * $scope.pageSize,
                                        Take: parseInt($scope.pageSize),
                                        Search: $scope.search,
                                        TimezoneOffset: new Date().getTimezoneOffset()
                                    }
                                };

                                var hasLocker = $scope.currentRequest !== null;
                                if (hasLocker) {
                                    $scope.currentRequest.cancel('tubularGrid(' + $scope.$id + '): new request coming.');
                                }

                                if (angular.isUndefined($scope.onBeforeGetData) === false)
                                    $scope.onBeforeGetData();

                                $scope.$emit('tbGrid_OnBeforeRequest', request);

                                $scope.currentRequest = $scope.gridDataService.retrieveDataAsync(request);

                                $scope.currentRequest.promise.then(
                                    function(data) {
                                        $scope.requestCounter += 1;

                                        if (angular.isUndefined(data) || data == null) {
                                            $scope.$emit('tbGrid_OnConnectionError', {
                                                statusText: "Data is empty",
                                                status: 0
                                            });

                                            return;
                                        }

                                        $scope.dataSource = data;

                                        $scope.rows = data.Payload.map(function(el) {
                                            var model = new TubularModel($scope, el, $scope.gridDataService);

                                            model.editPopup = function(template) {
                                                tubularPopupService.openDialog(template, model);
                                            };

                                            return model;
                                        });

                                        $scope.currentPage = data.CurrentPage;
                                        $scope.totalPages = data.TotalPages;
                                        $scope.totalRecordCount = data.TotalRecordCount;
                                        $scope.filteredRecordCount = data.FilteredRecordCount;
                                        $scope.isEmpty = $scope.filteredRecordCount == 0;
                                    }, function(error) {
                                        $scope.requestedPage = $scope.currentPage;
                                        $scope.$emit('tbGrid_OnConnectionError', error);
                                    }).then(function() {
                                    $scope.currentRequest = null;
                                });
                            };

                            $scope.$watch('hasColumnsDefinitions', function(newVal) {
                                if (newVal !== true) return;

                                var isGrouping = false;
                                // Check columns
                                angular.forEach($scope.columns, function(column) {
                                    if (column.IsGrouping) {
                                        if (isGrouping)
                                            throw 'Only one column is allowed to grouping';

                                        isGrouping = true;
                                        column.Visible = false;
                                        column.Sortable = true;
                                        column.SortOrder = 1;
                                        $scope.groupBy = column.Name;
                                    }
                                });

                                angular.forEach($scope.columns, function(column) {
                                    if ($scope.groupBy == column.Name) return;

                                    if (column.Sortable && column.SortOrder > 0)
                                        column.SortOrder++;
                                });

                                $scope.retrieveData();
                            });

                            $scope.$watch('pageSize', function() {
                                if ($scope.hasColumnsDefinitions && $scope.requestCounter > 0)
                                    $scope.retrieveData();
                            });

                            $scope.$watch('requestedPage', function() {
                                // TODO: we still need to inter-lock failed, initial and paged requests
                                if ($scope.hasColumnsDefinitions && $scope.requestCounter > 0)
                                    $scope.retrieveData();
                            });

                            $scope.sortColumn = function(columnName, multiple) {
                                var filterColumn = $scope.columns.filter(function(el) {
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
                                    angular.forEach($scope.columns.filter(function(col) { return col.Name !== columnName; }), function(col) {
                                        col.SortOrder = -1;
                                        col.SortDirection = 'None';
                                    });
                                }

                                // take the columns that actually need to be sorted in order to reindex them
                                var currentlySortedColumns = $scope.columns.filter(function(col) {
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

                                $scope.retrieveData();
                            };

                            $scope.selectedRows = function() {
                                var rows = localStorageService.get($scope.name + "_rows");
                                if (rows === null || rows === "") {
                                    rows = [];
                                }

                                return rows;
                            };

                            $scope.clearSelection = function() {
                                angular.forEach($scope.rows, function(value) {
                                    value.$selected = false;
                                });

                                localStorageService.set($scope.name + "_rows", []);
                            };

                            $scope.isEmptySelection = function() {
                                return $scope.selectedRows().length === 0;
                            };

                            $scope.selectFromSession = function(row) {
                                row.$selected = $scope.selectedRows().filter(function(el) {
                                    return el.$key === row.$key;
                                }).length > 0;
                            };

                            $scope.changeSelection = function(row) {
                                if (angular.isUndefined(row)) return;

                                row.$selected = !row.$selected;

                                var rows = $scope.selectedRows();

                                if (row.$selected) {
                                    rows.push({ $key: row.$key });
                                } else {
                                    rows = rows.filter(function(el) {
                                        return el.$key !== row.$key;
                                    });
                                }

                                localStorageService.set($scope.name + "_rows", rows);
                            };

                            $scope.getFullDataSource = function(callback) {
                                $scope.gridDataService.retrieveDataAsync({
                                    serverUrl: $scope.serverUrl,
                                    requestMethod: $scope.requestMethod,
                                    timeout: $scope.requestTimeout,
                                    requireAuthentication: $scope.requireAuthentication,
                                    data: {
                                        Count: $scope.requestCounter,
                                        Columns: $scope.columns,
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
                                    $scope.currentRequest = null;
                                });
                            };

                            $scope.$emit('tbGrid_OnGreetParentController', $scope);
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbGridTable
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
                            $scope.$component = $scope.$parent.$parent;
                            $scope.tubularDirective = 'tubular-grid-table';
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbColumnDefinitions
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
                    compile: function compile(cElement, cAttrs) {
                        return {
                            pre: function(scope, lElement, lAttrs, lController, lTransclude) {},
                            post: function(scope, lElement, lAttrs, lController, lTransclude) {
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
         * @restrict E
         *
         * @description
         * The `tbColumn` directive creates a column in the grid's model. 
         * All the attributes are used to generate a `ColumnModel`.
         * 
         * This directive is replace by a `th` HTML element.
         * TODO: Document ColumnModel attributes
         * @scope
         */
        .directive('tbColumn', [
            'tubulargGridColumnModel', function(ColumnModel) {
                return {
                    require: '^tbColumnDefinitions',
                    template: '<th ng-transclude ng-class="{sortable: column.Sortable}" ng-show="column.Visible"></th>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: true,
                    controller: [
                        '$scope', function($scope) {
                            $scope.column = { Label: '' };
                            $scope.$component = $scope.$parent.$parent.$component;
                            $scope.tubularDirective = 'tubular-column';

                            $scope.sortColumn = function(multiple) {
                                $scope.$component.sortColumn($scope.column.Name, multiple);
                            };
                        }
                    ],
                    compile: function compile(cElement, cAttrs) {
                        return {
                            pre: function(scope, lElement, lAttrs, lController, lTransclude) {
                                lAttrs.label = lAttrs.label || (lAttrs.name || '').replace(/([a-z])([A-Z])/g, '$1 $2');

                                var column = new ColumnModel(lAttrs);
                                scope.$component.addColumn(column);
                                scope.column = column;
                                scope.label = column.Label;
                            },
                            post: function(scope, lElement, lAttrs, lController, lTransclude) {
                            }
                        };
                    }
                };
            }
        ])
        /**
         * @ngdoc directive
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
            '$timeout', 'tubularConst', function($timeout, tubularConst) {

                return {
                    require: '^tbColumn',
                    template: '<a title="Click to sort. Press Ctrl to sort by multiple columns" ' +
                        'class="column-header" ng-transclude href="javascript:void(0)" ' +
                        'ng-click="sortColumn($event)"></a>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false,
                    controller: [
                        '$scope', function($scope) {
                            $scope.sortColumn = function($event) {
                                $scope.$parent.sortColumn($event.ctrlKey);
                            };
                        }
                    ],
                    compile: function compile(cElement, cAttrs) {
                        return {
                            pre: function(scope, lElement, lAttrs, lController, lTransclude) {
                                var refreshIcon = function(icon) {
                                    $(icon).removeClass(tubularConst.upCssClass);
                                    $(icon).removeClass(tubularConst.downCssClass);

                                    var cssClass = "";
                                    if (scope.$parent.column.SortDirection === 'Ascending')
                                        cssClass = tubularConst.upCssClass;

                                    if (scope.$parent.column.SortDirection === 'Descending')
                                        cssClass = tubularConst.downCssClass;

                                    $(icon).addClass(cssClass);
                                };

                                scope.$on('tbGrid_OnColumnSorted', function() {
                                    refreshIcon($('i.sort-icon.fa', lElement.parent()));
                                });

                                $timeout(function() {
                                    $(lElement).after('&nbsp;<i class="sort-icon fa"></i>');

                                    var icon = $('i.sort-icon.fa', lElement.parent());
                                    refreshIcon(icon);
                                }, 0);
                            },
                            post: function(scope, lElement, lAttrs, lController, lTransclude) {
                                scope.label = scope.$parent.label;

                                if (scope.$parent.column.Sortable == false) {
                                    var text = scope.label || lElement.text();
                                    lElement.replaceWith('<span>' + text + '</span>');
                                }
                            }
                        };
                    }
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbRowSet
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
         * @name tbRowTemplate
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
                    require: '^tbRowSet',
                    template: '<tr ng-transclude' +
                        ' ng-class="{\'info\': selectableBool && rowModel.$selected}"' +
                        ' ng-click="changeSelection(rowModel)"></tr>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: {
                        rowModel: '=',
                        selectable: '@'
                    },
                    controller: [
                        '$scope', function($scope) {
                            $scope.selectableBool = $scope.selectable !== "false";
                            $scope.$component = $scope.$parent.$parent.$parent.$component;

                            if ($scope.selectableBool && angular.isUndefined($scope.rowModel) === false) {
                                $scope.$component.selectFromSession($scope.rowModel);
                            }

                            $scope.changeSelection = function(rowModel) {
                                if ($scope.selectableBool == false) return;
                                $scope.$component.changeSelection(rowModel);
                            };
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbCellTemplate
         * @restrict E
         *
         * @description
         * The `tbCellTemplate` directive represents the final table element, a cell, where it can 
         * hold an inline editor or a plain AngularJS expression related to the current element in the `ngRepeat`.
         * 
         * This directive is replace by an `td` HTML element.
         * 
         * @scope
         * 
         * @param {string} columnName Setting the related column, by passing the name, the cell can share attributes (like visibiility) with the column.
         */
        .directive('tbCellTemplate', [
            function() {

                return {
                    require: '^tbRowTemplate',
                    template: '<td ng-transclude ng-show="column.Visible" data-label="{{column.Label}}"></td>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: {
                        columnName: '@?'
                    },
                    controller: [
                        '$scope', function($scope) {
                            $scope.column = { Visible: true };
                            $scope.columnName = $scope.columnName || null;
                            $scope.$component = $scope.$parent.$parent.$component;

                            if ($scope.columnName != null) {
                                var columnModel = $scope.$component.columns
                                    .filter(function(el) { return el.Name === $scope.columnName; });

                                if (columnModel.length > 0) {
                                    $scope.column = columnModel[0];
                                }
                            }
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbEmptyGrid
         * @restrict E
         *
         * @description
         * The `tbEmptyGrid` directive is a helper to show a "No records found" message when the grid has not rows.
         * 
         * This class must be inside a `tbRowSet` directive.
         */
        .directive('tbEmptyGrid', [
            function() {

                return {
                    require: '^tbGrid',
                    template: '<tr ngTransclude ng-show="$parent.$component.isEmpty">' +
                        '<td class="bg-warning" colspan="{{$parent.$component.columns.length + 1}}">' +
                        '<b>No records found</b>' +
                        '</td>' +
                        '</tr>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbRowGroupHeader
         * @restrict E
         *
         * @description
         * The `tbRowGroupHeader` directive is a cell template to show grouping information.
         * 
         * This class must be inside a `tbRowSet` directive.
         * 
         * @scope
         */
        .directive('tbRowGroupHeader', [
            function() {

                return {
                    require: '^tbRowTemplate',
                    template: '<td class="row-group" colspan="{{$parent.$component.columns.length + 1}}">' +
                        '<ng-transclude></ng-transclude>' +
                        '</td>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: {}
                };
            }
        ]);
})();
///#source 1 1 tubular/tubular-directives-gridcomponents.js
(function() {
    'use strict';

    angular.module('tubular.directives')
        /**
         * @ngdoc directive
         * @name tbTextSearch
         * @restrict E
         *
         * @description
         * The `tbTextSearch` directive is visual component to enable free-text search in a grid.
         * 
         * @scope
         */
        .directive('tbTextSearch', [function() {
            return {
                require: '^tbGrid',
                template:
                    '<div class="tubular-grid-search">' +
                        '<div class="input-group input-group-sm">' +
                        '<span class="input-group-addon"><i class="glyphicon glyphicon-search"></i></span>' +
                        '<input type="search" class="form-control" placeholder="search . . ." maxlength="20" ' +
                        'ng-model="$component.search.Text" ng-model-options="{ debounce: 300 }">' +
                        '<span class="input-group-btn" ng-show="$component.search.Text.length > 0" ng-click="$component.search.Text = \'\'">' +
                        '<button class="btn btn-default"><i class="fa fa-times-circle"></i></button>' +
                        '</span>' +
                        '<div>' +
                        '<div>',
                restrict: 'E',
                replace: true,
                transclude: false,
                scope: {},
                terminal: false,
                controller: [
                    '$scope', function($scope) {
                        $scope.$component = $scope.$parent.$parent;
                        $scope.tubularDirective = 'tubular-grid-text-search';
                        $scope.lastSearch = "";

                        $scope.$watch("$component.search.Text", function(val, prev) {
                            if (angular.isUndefined(val)) return;
                            if (val === prev) return;

                            if ($scope.lastSearch !== "" && val === "") {
                                $scope.$component.search.Operator = 'None';
                                $scope.$component.retrieveData();
                                return;
                            }

                            if (val === "" || val.length < 3) return;
                            if (val === $scope.lastSearch) return;

                            $scope.lastSearch = val;
                            $scope.$component.search.Operator = 'Auto';
                            $scope.$component.retrieveData();
                        });
                    }
                ]
            };
        }
        ])
        /**
         * @ngdoc directive
         * @name tbRemoveButton
         * @restrict E
         *
         * @description
         * The `tbRemoveButton` directive is visual helper to show a Remove button with a popover to confirm the action.
         * 
         * @scope
         * 
         * @param {object} model The row to remove.
         * @param {string} caption Set the caption to use in the button, default Remove.
         * @param {string} icon Set the CSS icon's class, the button can have only icon.
         */
        .directive('tbRemoveButton', ['$compile', function($compile) {

            return {
                require: '^tbGrid',
                template: '<button ng-click="confirmDelete()" class="btn" ng-hide="model.$isEditing">' +
                    '<span ng-show="showIcon" class="{{icon}}"></span>' +
                    '<span ng-show="showCaption">{{ caption || \'Remove\' }}</span>' +
                    '</button>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    model: '=',
                    caption: '@',
                    icon: '@'
                },
                controller: [
                    '$scope', '$element', function($scope, $element) {
                        $scope.showIcon = angular.isDefined($scope.icon);
                        $scope.showCaption = !($scope.showIcon && angular.isUndefined($scope.caption));
                        $scope.confirmDelete = function() {
                            $element.popover({
                                html: true,
                                title: 'Do you want to delete this row?',
                                content: function() {
                                    var html = '<div class="tubular-remove-popover">' +
                                        '<button ng-click="model.delete()" class="btn btn-danger btn-xs">Remove</button>' +
                                        '&nbsp;<button ng-click="cancelDelete()" class="btn btn-default btn-xs">Cancel</button>' +
                                        '</div>';
                                    return $compile(html)($scope);
                                }
                            });

                            $element.popover('show');
                        };

                        $scope.cancelDelete = function() {
                            $element.popover('destroy');
                        };
                    }
                ]
            };
        }
        ])
        /**
         * @ngdoc directive
         * @name tbSaveButton
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
        .directive('tbSaveButton', [function() {

            return {
                require: '^tbGrid',
                template: '<div ng-show="model.$isEditing"><button ng-click="save()" class="btn btn-default {{ saveCss || \'\' }}" ' +
                    'ng-disabled="!model.$valid()">' +
                    '{{ saveCaption || \'Save\' }}' +
                    '</button>' +
                    '<button ng-click="cancel()" class="btn {{ cancelCss || \'btn-default\' }}">' +
                    '{{ cancelCaption || \'Cancel\' }}' +
                    '</button></div>',
                restrict: 'E',
                replace: true,
                transclude: true,
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

                            $scope.currentRequest = $scope.model.save();

                            if ($scope.currentRequest === false) {
                                $scope.$emit('tbGrid_OnSavingNoChanges', $scope.model);
                                return;
                            }

                            $scope.currentRequest.then(
                                function(data) {
                                    $scope.model.$isEditing = false;
                                    $scope.$emit('tbGrid_OnSuccessfulSave', data);
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
         * @ngdoc directive
         * @name tbEditButton
         * @restrict E
         *
         * @description
         * The `tbEditButton` directive is visual helper to create an Edit button.
         * 
         * @scope
         * 
         * @param {object} model The row to remove.
         * @param {string} caption Set the caption to use in the button, default Edit.
         * @param {string} css Add a CSS class to the button.
         */
        .directive('tbEditButton', [function() {

            return {
                require: '^tbGrid',
                template: '<button ng-click="edit()" class="btn btn-default {{ css || \'\' }}" ' +
                    'ng-hide="model.$isEditing">{{ caption || \'Edit\' }}</button>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    model: '=',
                    caption: '@',
                    css: '@'
                },
                controller: [
                    '$scope', function($scope) {
                        $scope.component = $scope.$parent.$parent.$component;
                        $scope.edit = function() {
                            if ($scope.component.editorMode == 'popup') {
                                $scope.model.editPopup();
                            } else {
                                $scope.model.edit();
                            }
                        };
                    }
                ]
            };
        }
        ])
        /**
         * @ngdoc directive
         * @name tbPageSizeSelector
         * @restrict E
         *
         * @description
         * The `tbPageSizeSelector` directive is visual helper to render a dropdown to allow user select how many rows by page.
         * 
         * @scope
         * 
         * @param {string} caption Set the caption to use in the button, default "Page size:".
         * @param {string} css Add a CSS class to the `div` HTML element.
         * @param {string} selectorCss Add a CSS class to the `select` HTML element.
         * @param {array} options Set the page options array, default ['10', '20', '50', '100'].
         */
        .directive('tbPageSizeSelector', [function() {

            return {
                require: '^tbGrid',
                template: '<div class="{{css}}"><form class="form-inline">' +
                    '<div class="form-group">' +
                    '<label class="small">{{ caption || \'Page size:\' }}</label>' +
                    '<select ng-model="$parent.$parent.pageSize" class="form-control input-sm {{selectorCss}}" ' +
                    'ng-options="item for item in options">' +
                    '</select>' +
                    '</div>' +
                    '</form></div>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    caption: '@',
                    css: '@',
                    selectorCss: '@',
                    options: '=?'
                },
                controller: [
                    '$scope', function($scope) {
                        $scope.options = angular.isDefined($scope.options) ? $scope.options : ['10', '20', '50', '100'];
                    }
                ]
            };
        }
        ])
        /**
         * @ngdoc directive
         * @name tbExportButton
         * @restrict E
         *
         * @description
         * The `tbExportButton` directive is visual helper to render a button to export grid to CSV format.
         * 
         * @scope
         * 
         * @param {string} filename Set the export file name.
         * @param {string} css Add a CSS class to the `button` HTML element.
         */
        .directive('tbExportButton', [function() {

            return {
                require: '^tbGrid',
                template: '<div class="btn-group">' +
                    '<button class="btn btn-default dropdown-toggle {{css}}" data-toggle="dropdown" aria-expanded="false">' +
                    '<span class="fa fa-download"></span>&nbsp;Export CSV&nbsp;<span class="caret"></span>' +
                    '</button>' +
                    '<ul class="dropdown-menu" role="menu">' +
                    '<li><a href="javascript:void(0)" ng-click="downloadCsv($parent)">Current rows</a></li>' +
                    '<li><a href="javascript:void(0)" ng-click="downloadAllCsv($parent)">All rows</a></li>' +
                    '</ul>' +
                    '</div>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    filename: '@',
                    css: '@'
                },
                controller: [
                    '$scope', 'tubularGridExportService', function($scope, tubularGridExportService) {
                        $scope.$component = $scope.$parent.$parent;

                        $scope.downloadCsv = function() {
                            tubularGridExportService.exportGridToCsv($scope.filename, $scope.$component);
                        };

                        $scope.downloadAllCsv = function() {
                            tubularGridExportService.exportAllGridToCsv($scope.filename, $scope.$component);
                        };
                    }
                ]
            };
        }
        ])
        /**
         * @ngdoc directive
         * @name tbPrintButton
         * @restrict E
         *
         * @description
         * The `tbPrintButton` directive is visual helper to render a button to print the `tbGrid`.
         * 
         * @scope
         * 
         * @param {string} title Set the document's title.
         * @param {string} printCss Set a stylesheet URL to attach to print mode.
         */
        .directive('tbPrintButton', [function() {

            return {
                require: '^tbGrid',
                template: '<button class="btn btn-default" ng-click="printGrid()">' +
                    '<span class="fa fa-print"></span>&nbsp;Print' +
                    '</button>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    title: '@',
                    printCss: '@'
                },
                controller: [
                    '$scope', function($scope) {
                        $scope.$component = $scope.$parent.$parent;

                        $scope.printGrid = function() {
                            $scope.$component.getFullDataSource(function(data) {
                                var tableHtml = "<table class='table table-bordered table-striped'><thead><tr>"
                                    + $scope.$component.columns.map(function(el) {
                                        return "<th>" + (el.Label || el.Name) + "</th>";
                                    }).join(" ")
                                    + "</tr></thead>"
                                    + "<tbody>"
                                    + data.map(function(row) {
                                        if (typeof (row) === 'object')
                                            row = $.map(row, function(el) { return el; });

                                        return "<tr>" + row.map(function(cell) { return "<td>" + cell + "</td>"; }).join(" ") + "</tr>";
                                    }).join(" ")
                                    + "</tbody>"
                                    + "</table>";

                                var popup = window.open("about:blank", "Print", "menubar=0,location=0,height=500,width=800");
                                popup.document.write('<link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/3.3.1/css/bootstrap.min.css" />');

                                if ($scope.printCss != '')
                                    popup.document.write('<link rel="stylesheet" href="' + $scope.printCss + '" />');

                                popup.document.write('<body onload="window.print();">');
                                popup.document.write('<h1>' + $scope.title + '</h1>');
                                popup.document.write(tableHtml);
                                popup.document.write('</body>');
                                popup.document.close();
                            });
                        };
                    }
                ]
            };
        }
    ]);
})();
///#source 1 1 tubular/tubular-directives-gridpager.js
(function () {
    'use strict';

    angular.module('tubular.directives')
        /**
         * @ngdoc directive
         * @name tbGridPager
         * @restrict E
         *
         * @description
         * The `tbGridPager` directive generates a pager connected to the parent `tbGrid`.
         * 
         * @scope
         */
        .directive('tbGridPager', [
            '$timeout', function ($timeout) {
                return {
                    require: '^tbGrid',
                    template:
                        '<div class="tubular-pager">' +
                            '<pagination ng-disabled="$component.isEmpty" direction-links="true" ' +
                            'boundary-links="true" total-items="$component.filteredRecordCount" ' +
                            'items-per-page="$component.pageSize" max-size="5" ng-model="pagerPageNumber" ng-change="pagerPageChanged()">' +
                            '</pagination>' +
                            '<div>',
                    restrict: 'E',
                    replace: true,
                    transclude: false,
                    scope: true,
                    terminal: false,
                    controller: [
                        '$scope', '$element', function ($scope, $element) {
                            $scope.$component = $scope.$parent.$parent;
                            $scope.tubularDirective = 'tubular-grid-pager';

                            $scope.$component.$watch('currentPage', function (value) {
                                $scope.pagerPageNumber = value;
                            });

                            $scope.pagerPageChanged = function () {
                                $scope.$component.requestedPage = $scope.pagerPageNumber;
                                var allLinks = $element.find('li a');
                                $(allLinks).blur();
                            };
                        }
                    ],
                    compile: function compile(cElement, cAttrs) {
                        return {
                            pre: function (scope, lElement, lAttrs, lController, lTransclude) { },
                            post: function (scope, lElement, lAttrs, lController, lTransclude) {
                                scope.firstButtonClass = lAttrs.firstButtonClass || 'fa fa-fast-backward';
                                scope.prevButtonClass = lAttrs.prevButtonClass || 'fa fa-backward';

                                scope.nextButtonClass = lAttrs.nextButtonClass || 'fa fa-forward';
                                scope.lastButtonClass = lAttrs.lastButtonClass || 'fa fa-fast-forward';

                                $timeout(function () {
                                    var allLinks = lElement.find('li a');

                                    $(allLinks[0]).html('<i class="' + scope.firstButtonClass + '"></i>');
                                    $(allLinks[1]).html('<i class="' + scope.prevButtonClass + '"></i>');

                                    $(allLinks[allLinks.length - 2]).html('<i class="' + scope.nextButtonClass + '"></i>');
                                    $(allLinks[allLinks.length - 1]).html('<i class="' + scope.lastButtonClass + '"></i>');
                                }, 0);

                            }
                        };
                    }
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbGridPagerInfo
         * @restrict E
         *
         * @description
         * The `tbGridPagerInfo` directive shows how many records are shown in a page and total rows.
         * 
         * @scope
         */
        .directive('tbGridPagerInfo', [
            function () {
                return {
                    require: '^tbGrid',
                    template: '<div class="pager-info small">Showing {{currentInitial}} ' +
                        'to {{currentTop}} ' +
                        'of {{$component.filteredRecordCount}} records ' +
                        '<span ng-show="filtered">' +
                        '(Filtered from {{$component.totalRecordCount}} total records)</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: true,
                    controller: [
                        '$scope', function ($scope) {
                            $scope.$component = $scope.$parent.$parent;
                            $scope.fixCurrentTop = function () {
                                $scope.currentTop = $scope.$component.pageSize * $scope.$component.currentPage;
                                $scope.currentInitial = (($scope.$component.currentPage - 1) * $scope.$component.pageSize) + 1;

                                if ($scope.currentTop > $scope.$component.filteredRecordCount) {
                                    $scope.currentTop = $scope.$component.filteredRecordCount;
                                }

                                if ($scope.currentTop < 0) {
                                    $scope.currentTop = 0;
                                }

                                if ($scope.currentInitial < 0) {
                                    $scope.currentInitial = 0;
                                }
                            };

                            $scope.$component.$watch('filteredRecordCount', function () {
                                $scope.filtered = $scope.$component.totalRecordCount != $scope.$component.filteredRecordCount;
                                $scope.fixCurrentTop();
                            });

                            $scope.$component.$watch('currentPage', function () {
                                $scope.fixCurrentTop();
                            });

                            $scope.$component.$watch('pageSize', function () {
                                $scope.fixCurrentTop();
                            });

                            $scope.fixCurrentTop();
                        }
                    ]
                };
            }
        ]);
})();
///#source 1 1 tubular/tubular-directives-editors.js
(function() {
    'use strict';

    angular.module('tubular.directives')
        /**
         * @ngdoc directive
         * @name tbSimpleEditor
         * @restrict E
         *
         * @description
         * The `tbSimpleEditor` directive is the basic input to show in a grid or form.
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * TODO: Define attributes
         * 
         * @scope
         */
        .directive('tbSimpleEditor', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<div ng-class="{ \'form-group\' : isEditing, \'has-error\' : !$valid }">' +
                        '<span ng-hide="isEditing">{{value}}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<input type="{{editorType}}" placeholder="{{placeholder}}" ng-show="isEditing" ng-model="value" class="form-control" ' +
                        ' ng-required="required" ng-readonly="readOnly" />' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">{{error}}</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: tubularEditorService.defaultScope,
                    controller: [
                        '$scope', function($scope) {
                            $scope.validate = function() {
                                if (angular.isUndefined($scope.min) == false && angular.isUndefined($scope.value) == false) {
                                    if ($scope.value.length < parseInt($scope.min)) {
                                        $scope.$valid = false;
                                        $scope.state.$errors = ["The fields needs to be minimum " + $scope.min + " chars"];
                                        return;
                                    }
                                }

                                if (angular.isUndefined($scope.max) == false && angular.isUndefined($scope.value) == false) {
                                    if ($scope.value.length > parseInt($scope.max)) {
                                        $scope.$valid = false;
                                        $scope.state.$errors = ["The fields needs to be maximum " + $scope.min + " chars"];
                                        return;
                                    }
                                }
                            };

                            tubularEditorService.setupScope($scope);
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbNumericEditor
         * @restrict E
         *
         * @description
         * The `tbNumericEditor` directive is numeric input, similar to `tbSimpleEditor` 
         * but can render an addon to the input visual element.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @scope
         */
        .directive('tbNumericEditor', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<div ng-class="{ \'form-group\' : isEditing, \'has-error\' : !$valid }">' +
                        '<span ng-hide="isEditing">{{value | numberorcurrency: format }}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<div class="input-group" ng-show="isEditing">' +
                        '<div class="input-group-addon" ng-show="format == \'C\'">$</div>' +
                        '<input type="number" placeholder="{{placeholder}}" ng-model="value" class="form-control" ' +
                        'ng-required="required" ng-readonly="readOnly" />' +
                        '</div>' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">{{error}}</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: tubularEditorService.defaultScope,
                    controller: [
                        '$scope', function($scope) {
                            $scope.validate = function() {
                                if (angular.isUndefined($scope.min) == false && angular.isUndefined($scope.value) == false) {
                                    $scope.$valid = $scope.value >= $scope.min;
                                    if ($scope.$valid == false)
                                        $scope.state.$errors = ["The minimum is " + $scope.min];
                                }

                                if ($scope.$valid == false) return;

                                if (angular.isUndefined($scope.max) == false && angular.isUndefined($scope.value) == false) {
                                    $scope.$valid = $scope.value <= $scope.max;
                                    if ($scope.$valid == false)
                                        $scope.state.$errors = ["The maximum is " + $scope.max];
                                }
                            };

                            tubularEditorService.setupScope($scope, 0);
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbDateTimeEditor
         * @restrict E
         *
         * @description
         * The `tbDateTimeEditor` directive is date/time input. It uses the `datetime-local` HTML5 attribute, but if this
         * components fails it falls back to a jQuery datepicker.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @scope
         */
        .directive('tbDateTimeEditor', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<div ng-class="{ \'form-group\' : isEditing }">' +
                        '<span ng-hide="isEditing">{{ value | date: format }}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<input type="datetime-local" ng-show="isEditing" ng-model="value" class="form-control" ' +
                        'ng-required="required" ng-readonly="readOnly" />' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">' +
                        '{{error}}' +
                        '</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: tubularEditorService.defaultScope,
                    controller: [
                        '$scope', function($scope) {
                            $scope.DataType = "date";

                            $scope.validate = function() {
                                if (angular.isUndefined($scope.min) == false) {
                                    $scope.$valid = $scope.value >= $scope.min;
                                    if ($scope.$valid == false)
                                        $scope.state.$errors = ["The minimum is " + $scope.min];
                                }

                                if ($scope.$valid == false) return;

                                if (angular.isUndefined($scope.max) == false) {
                                    $scope.$valid = $scope.value <= $scope.max;
                                    if ($scope.$valid == false)
                                        $scope.state.$errors = ["The maximum is " + $scope.max];
                                }
                            };

                            tubularEditorService.setupScope($scope, 'yyyy-MM-dd HH:mm');
                        }
                    ],
                    compile: function compile(cElement, cAttrs) {
                        return {
                            pre: function(scope, lElement, lAttrs, lController, lTransclude) {},
                            post: function(scope, lElement, lAttrs, lController, lTransclude) {
                                var inp = $(lElement).find("input[type=datetime-local]")[0];
                                if (inp.type !== 'datetime-local') {
                                    $(inp).datepicker({
                                        dateFormat: scope.format.toLowerCase()
                                    }).on("dateChange", function(e) {
                                        scope.value = e.date;
                                        scope.$parent.Model.$hasChanges = true;
                                    });
                                }
                            }
                        };
                    }
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbDateEditor
         * @restrict E
         *
         * @description
         * The `tbDateEditor` directive is date input. It uses the `datetime-local` HTML5 attribute, but if this
         * components fails it falls back to a jQuery datepicker.
         * 
         * Similar to `tbDateTimeEditor` but without a timepicker.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @scope
         */
        .directive('tbDateEditor', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<div ng-class="{ \'form-group\' : isEditing }">' +
                        '<span ng-hide="isEditing">{{ value | date: format }}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<input type="date" ng-show="isEditing" ng-model="value" class="form-control" ' +
                        'ng-required="required" ng-readonly="readOnly" />' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">' +
                        '{{error}}' +
                        '</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: tubularEditorService.defaultScope,
                    controller: [
                        '$scope', function($scope) {
                            $scope.DataType = "date";

                            $scope.validate = function() {
                                $scope.validate = function() {
                                    if (angular.isUndefined($scope.min) == false) {
                                        $scope.$valid = $scope.value >= $scope.min;
                                        if ($scope.$valid == false)
                                            $scope.state.$errors = ["The minimum is " + $scope.min];
                                    }

                                    if ($scope.$valid == false) return;

                                    if (angular.isUndefined($scope.max) == false) {
                                        $scope.$valid = $scope.value <= $scope.max;
                                        if ($scope.$valid == false)
                                            $scope.state.$errors = ["The maximum is " + $scope.max];
                                    }
                                };

                                if ($scope.value == null) { // TODO: This is not working :P
                                    $scope.$valid = false;
                                    $scope.state.$errors = ["Invalid date"];
                                    return;
                                }
                            };

                            tubularEditorService.setupScope($scope, 'yyyy-MM-dd');
                        }
                    ],
                    compile: function compile(cElement, cAttrs) {
                        return {
                            pre: function(scope, lElement, lAttrs, lController, lTransclude) {},
                            post: function(scope, lElement, lAttrs, lController, lTransclude) {
                                var inp = $(lElement).find("input[type=date]")[0];
                                if (inp.type != 'date') {
                                    $(inp).datepicker({
                                        dateFormat: scope.format.toLowerCase()
                                    }).on("dateChange", function(e) {
                                        scope.value = e.date;
                                        scope.$parent.Model.$hasChanges = true;
                                    });
                                }
                            }
                        };
                    }
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbDropdownEditor
         * @restrict E
         *
         * @description
         * The `tbDropdownEditor` directive is drowpdown editor, it can get information from a HTTP 
         * source or it can be an object declared in the attributes.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @scope
         */
        .directive('tbDropdownEditor', [
            'tubularEditorService', 'tubularHttp', function(tubularEditorService, tubularHttp) {

                return {
                    template: '<div ng-class="{ \'form-group\' : isEditing, \'has-error\' : !$valid }">' +
                        '<span ng-hide="isEditing">{{ value }}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<select ng-options="d for d in options" ng-show="isEditing" ng-model="value" class="form-control" ' +
                        'ng-required="required" />' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">' +
                        '{{error}}' +
                        '</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: angular.extend({ options: '=?', optionsUrl: '@', optionsMethod: '@?' }, tubularEditorService.defaultScope),
                    controller: [
                        '$scope', function($scope) {
                            tubularEditorService.setupScope($scope);
                            $scope.$editorType = 'select';
                            $scope.dataIsLoaded = false;

                            $scope.loadData = function() {
                                if ($scope.dataIsLoaded) return;

                                var currentRequest = tubularHttp.retrieveDataAsync({
                                    serverUrl: $scope.optionsUrl,
                                    requestMethod: $scope.optionsMethod || 'GET'
                                });

                                var value = $scope.value;
                                $scope.value = '';

                                currentRequest.promise.then(
                                    function(data) {
                                        $scope.options = data;
                                        $scope.dataIsLoaded = true;
                                        $scope.value = value;
                                    }, function(error) {
                                        $scope.$emit('tbGrid_OnConnectionError', error);
                                    });
                            };

                            if (angular.isUndefined($scope.optionsUrl) == false) {
                                if ($scope.isEditing) {
                                    $scope.loadData();
                                } else {
                                    $scope.$watch('isEditing', function() {
                                        if ($scope.isEditing) $scope.loadData();
                                    });
                                }
                            }
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbTypeaheadEditor
         * @restrict E
         *
         * @description
         * The `tbTypeaheadEditor` directive is autocomplete editor, it can get information from a HTTP source or it can get them
         * from a object declared in the attributes.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @scope
         */
        .directive('tbTypeaheadEditor', [
            'tubularEditorService', 'tubularHttp', '$q', function(tubularEditorService, tubularHttp, $q) {

                return {
                    template: '<div ng-class="{ \'form-group\' : isEditing, \'has-error\' : !$valid }">' +
                        '<span ng-hide="isEditing">{{ value }}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<input ng-show="isEditing" ng-model="value" class="form-control" typeahead="o for o in getValues($viewValue)" ' +
                        'ng-required="required" />' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">' +
                        '{{error}}' +
                        '</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: angular.extend({
                        options: '=?',
                        optionsUrl: '@',
                        optionsMethod: '@?'
                    }, tubularEditorService.defaultScope),
                    controller: [
                        '$scope', function($scope) {
                            tubularEditorService.setupScope($scope);
                            $scope.$editorType = 'select';

                            $scope.getValues = function(val) {
                                if (angular.isDefined($scope.optionsUrl)) {
                                    return tubularHttp.retrieveDataAsync({
                                        serverUrl: $scope.optionsUrl + '?search=' + val,
                                        requestMethod: $scope.optionsMethod || 'GET'
                                    }).promise;
                                }

                                return $q(function(resolve) {
                                    resolve($scope.options);
                                });
                            };
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbHiddenField
         * @restrict E
         *
         * @description
         * The `tbHiddenField` directive represents a hidden field.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @scope
         */
        .directive('tbHiddenField', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<input type="hidden" ng-show="isEditing" ng-model="value" class="form-control"  />',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: tubularEditorService.defaultScope,
                    controller: [
                        '$scope', function($scope) {
                            tubularEditorService.setupScope($scope);
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbCheckboxField
         * @restrict E
         *
         * @description
         * The `tbCheckboxField` directive represents a checkbox field.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @scope
         */
        .directive('tbCheckboxField', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<div ng-class="{ \'form-group\' : isEditing, \'has-error\' : !$valid }">' +
                        '<span ng-hide="isEditing">{{value}}</span>' +
                        '<label>' +
                        '<input type="checkbox" ng-show="isEditing" ng-model="value" ng-required="required" /> ' +
                        '<span ng-show="showLabel">{{label}}</span>' +
                        '</label>' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">' +
                        '{{error}}' +
                        '</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: tubularEditorService.defaultScope,
                    controller: [
                        '$scope', function($scope) {
                            tubularEditorService.setupScope($scope);
                        }
                    ]
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbTextArea
         * @restrict E
         *
         * @description
         * The `tbTextArea` directive represents a textarea field. 
         * Similar to `tbSimpleEditor` but with a `textarea` HTML element instead of `input`.
         * 
         * It uses the `TubularModel` to retrieve column or field information.
         * 
         * @scope
         */
        .directive('tbTextArea', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<div ng-class="{ \'form-group\' : isEditing, \'has-error\' : !$valid }">' +
                        '<span ng-hide="isEditing">{{value}}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<textarea ng-show="isEditing" placeholder="{{placeholder}}" ng-model="value" class="form-control" ' +
                        ' ng-required="required" ng-readonly="readOnly"></textarea>' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">' +
                        '{{error}}' +
                        '</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: tubularEditorService.defaultScope,
                    controller: [
                        '$scope', function($scope) {
                            $scope.validate = function() {
                                if (angular.isUndefined($scope.min) == false && angular.isUndefined($scope.value) == false) {
                                    if ($scope.value.length < parseInt($scope.min)) {
                                        $scope.$valid = false;
                                        $scope.state.$errors = ["The fields needs to be minimum " + $scope.min + " chars"];
                                        return;
                                    }
                                }

                                if (angular.isUndefined($scope.max) == false && angular.isUndefined($scope.value) == false) {
                                    if ($scope.value.length > parseInt($scope.max)) {
                                        $scope.$valid = false;
                                        $scope.state.$errors = ["The fields needs to be maximum " + $scope.min + " chars"];
                                        return;
                                    }
                                }
                            };

                            tubularEditorService.setupScope($scope);
                        }
                    ]
                };
            }
        ]);
})();
///#source 1 1 tubular/tubular-directives-filters.js
(function() {
    'use strict';

    angular.module('tubular.directives')
        /**
         * @ngdoc directive
         * @name tbColumnFilterButtons
         * @restrict E
         *
         * @description
         * The `tbColumnFilterButtons` is an internal directive, and it is used to show basic filtering buttons.
         */
        .directive('tbColumnFilterButtons', [function () {
            return {
                template: '<div class="btn-group"><a class="btn btn-sm btn-success" ng-click="applyFilter()">Apply</a>' +
                        '<button class="btn btn-sm btn-danger" ng-click="clearFilter()">Clear</button>' +
                        '<button class="btn btn-sm btn-default" ng-click="close()">Close</button>' +
                        '</div>',
                restrict: 'E',
                replace: true,
                transclude: true,
            };
        }])
        /**
         * @ngdoc directive
         * @name tbColumnFilterColumnSelector
         * @restrict E
         *
         * @description
         * The `tbColumnFilterColumnSelector` is an internal directive, and it is used to show columns selector popup.
         */
        .directive('tbColumnFilterColumnSelector', [function() {
            return {
                template: '<div><hr /><h4>Columns Selector</h4><button class="btn btn-sm btn-default" ng-click="openColumnsSelector()">Select Columns</button></div>',
                restrict: 'E',
                replace: true,
                transclude: true,
            };
        }])
        /**
         * @ngdoc directive
         * @name tbColumnFilter
         * @restrict E
         *
         * @description
         * The `tbColumnFilter` directive is a the basic filter popover. You need to define it inside a `tbColumn`.
         * 
         * The parent scope will provide information about the data type.
         * TODO: List params from tubularGridFilterService
         */
        .directive('tbColumnFilter', [
            'tubularGridFilterService', function(tubularGridFilterService) {

                return {
                    require: '^tbColumn',
                    template: '<div class="tubular-column-menu">' +
                        '<button class="btn btn-xs btn-default" data-toggle="popover" data-placement="bottom" ' +
                        'ng-class="{ \'btn-success\': (filter.Operator !== \'None\' && filter.Text.length > 0) }">' +
                        '<i class="fa fa-filter"></i></button>' +
                        '<div style="display: none;">' +
                        '<h4>{{filterTitle}}</h4>' +
                        '<form class="tubular-column-filter-form" onsubmit="return false;">' +
                        '<select class="form-control" ng-model="filter.Operator" ng-hide="dataType == \'boolean\'"></select>' +
                        '<input class="form-control" type="{{ dataType == \'boolean\' ? \'checkbox\' : \'search\'}}" ng-model="filter.Text" placeholder="Value" ' +
                        'ng-disabled="filter.Operator == \'None\'" />' +
                        '<input type="search" class="form-control" ng-model="filter.Argument[0]" ng-show="filter.Operator == \'Between\'" />' +
                        '<hr />' +
                        '<tb-column-filter-buttons></tb-column-filter-buttons>' +
                        '<tb-column-filter-column-selector ng-show="columnSelector"></tb-column-filter-column-selector>' +
                        '</form></div>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false,
                    compile: function compile(cElement, cAttrs) {
                        return {
                            pre: function(scope, lElement, lAttrs, lController, lTransclude) {
                                tubularGridFilterService.applyFilterFuncs(scope, lElement, lAttrs);
                            },
                            post: function(scope, lElement, lAttrs, lController, lTransclude) {
                                tubularGridFilterService.createFilterModel(scope, lAttrs);
                            }
                        };
                    }
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbColumnDateTimeFilter
         * @restrict E
         *
         * @description
         * The `tbColumnDateTimeFilter` directive is a specific filter with Date and Time editors, instead regular inputs.
         * 
         * The parent scope will provide information about the data type.
         * 
         * TODO: List params from tubularGridFilterService
         */
        .directive('tbColumnDateTimeFilter', [
            'tubularGridFilterService', function(tubularGridFilterService) {

                return {
                    require: '^tbColumn',
                    template: '<div ngTransclude class="btn-group tubular-column-filter">' +
                        '<button class="tubular-column-filter-button btn btn-xs btn-default" data-toggle="popover" data-placement="bottom" ' +
                        'ng-class="{ \'btn-success\': filter.Text != null }">' +
                        '<i class="fa fa-filter"></i></button>' +
                        '<div style="display: none;">' +
                        '<h4>{{filterTitle}}</h4>' +
                        '<form class="tubular-column-filter-form" onsubmit="return false;">' +
                        '<select class="form-control" ng-model="filter.Operator"></select>' +
                        '<input type="date" class="form-control" ng-model="filter.Text" />' +
                        '<input type="date" class="form-control" ng-model="filter.Argument[0]" ng-show="filter.Operator == \'Between\'" />' +
                        '<hr />' +
                        '<tb-column-filter-buttons></tb-column-filter-buttons>' +
                        '<tb-column-filter-column-selector ng-show="columnSelector"></tb-column-filter-column-selector>' +
                        '</form></div>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false,
                    controller: [
                        '$scope', function($scope) {
                            $scope.filter = {};

                            $scope.format = 'yyyy-MM-dd';
                        }
                    ],
                    compile: function compile(cElement, cAttrs) {
                        return {
                            pre: function(scope, lElement, lAttrs, lController, lTransclude) {
                                tubularGridFilterService.applyFilterFuncs(scope, lElement, lAttrs, function() {
                                    var inp = $(lElement).find("input[type=date]")[0];

                                    if (inp.type != 'date') {
                                        $(inp).datepicker({
                                            dateFormat: scope.format.toLowerCase()
                                        }).on("dateChange", function(e) {
                                            scope.filter.Text = e.date;
                                        });
                                    }

                                    var inpLev = $(lElement).find("input[type=date]")[1];

                                    if (inpLev.type != 'date') {
                                        $(inpLev).datepicker({
                                            dateFormat: scope.format.toLowerCase()
                                        }).on("dateChange", function(e) {
                                            scope.filter.Argument = [e.date];
                                        });
                                    }
                                });
                            },
                            post: function(scope, lElement, lAttrs, lController, lTransclude) {
                                tubularGridFilterService.createFilterModel(scope, lAttrs);
                            }
                        };
                    }
                };
            }
        ])
        /**
         * @ngdoc directive
         * @name tbColumnOptionsFilter
         * @restrict E
         *
         * @description
         * The `tbColumnOptionsFilter` directive is a filter with an dropdown listing all the possible values to filter.
         * 
         * TODO: List params from tubularGridFilterService
         */
        .directive('tbColumnOptionsFilter', [
            'tubularGridFilterService', 'tubularHttp', function(tubularGridFilterService, tubularHttp) {

                return {
                    require: '^tbColumn',
                    template: '<div class="tubular-column-filter">' +
                        '<button class="tubular-column-filter-button btn btn-xs btn-default" data-toggle="popover" data-placement="bottom" ' +
                        'ng-class="{ \'btn-success\': (filter.Argument.length > 0) }">' +
                        '<i class="fa fa-filter"></i></button>' +
                        '<div style="display: none;">' +
                        '<h4>{{filterTitle}}</h4>' +
                        '<form class="tubular-column-filter-form" onsubmit="return false;">' +
                        '<select class="form-control" ng-model="filter.Argument" ng-options="item for item in optionsItems" multiple></select>' +
                        '<hr />' + // Maybe we should add checkboxes or something like that
                        '<tb-column-filter-buttons></tb-column-filter-buttons>' +
                        '<tb-column-filter-column-selector ng-show="columnSelector"></tb-column-filter-column-selector>' +
                        '</form></div>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false,
                    controller: [
                        '$scope', function($scope) {
                            $scope.dataIsLoaded = false;

                            $scope.getOptionsFromUrl = function() {
                                if ($scope.dataIsLoaded) return;

                                var currentRequest = tubularHttp.retrieveDataAsync({
                                    serverUrl: $scope.filter.OptionsUrl,
                                    requestMethod: 'GET'
                                });

                                currentRequest.promise.then(
                                    function(data) {
                                        $scope.optionsItems = data;
                                        $scope.dataIsLoaded = true;
                                    }, function(error) {
                                        $scope.$emit('tbGrid_OnConnectionError', error);
                                    });
                            };
                        }
                    ],
                    compile: function compile(cElement, cAttrs) {
                        return {
                            pre: function(scope, lElement, lAttrs, lController, lTransclude) {
                                tubularGridFilterService.applyFilterFuncs(scope, lElement, lAttrs,function() {
                                    scope.getOptionsFromUrl();
                                });
                            },
                            post: function(scope, lElement, lAttrs, lController, lTransclude) {
                                tubularGridFilterService.createFilterModel(scope, lAttrs);

                                scope.filter.Operator = 'Multiple';
                            }
                        };
                    }
                };
            }
        ]);
})();
///#source 1 1 tubular/tubular-directives-forms.js
(function() {
    'use strict';

    angular.module('tubular.directives')
        /**
         * @ngdoc directive
         * @name tbForm
         * @restrict E
         *
         * @description
         * The `tbForm` directive is the base to create any form.
         * 
         * @scope
         */
        .directive('tbForm', [
            function() {
                return {
                    template: '<form ng-transclude></form>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: {
                        model: '=?',
                        serverUrl: '@',
                        serverSaveUrl: '@',
                        serverSaveMethod: '@',
                        isNew: '@',
                        modelKey: '@?',
                        gridDataService: '=?service',
                        gridDataServiceName: '@?serviceName',
                        requireAuthentication: '=?'
                    },
                    controller: [
                        '$scope', '$routeParams', 'tubularModel', 'tubularHttp', 'tubularOData',
                        function($scope, $routeParams, TubularModel, tubularHttp, tubularOData) {
                            $scope.tubularDirective = 'tubular-form';
                            $scope.serverSaveMethod = $scope.serverSaveMethod || 'POST';
                            $scope.fields = [];
                            $scope.hasFieldsDefinitions = false;
                            // Try to load a key from markup or route
                            $scope.modelKey = $scope.modelKey || $routeParams.param;

                            $scope.gridDataService = $scope.gridDataService || tubularHttp;

                            // Helper to use OData without controller
                            if ($scope.gridDataServiceName === 'odata') {
                                $scope.gridDataService = tubularOData;
                            }

                            // Setup require authentication
                            $scope.requireAuthentication = angular.isUndefined($scope.requireAuthentication) ? true : $scope.requireAuthentication;
                            $scope.gridDataService.setRequireAuthentication($scope.requireAuthentication);

                            $scope.addField = function(item) {
                                if (item.name === null) return;

                                if ($scope.hasFieldsDefinitions !== false)
                                    throw 'Cannot define more fields. Field definitions have been sealed';

                                item.Name = item.name;
                                $scope.fields.push(item);
                            };

                            $scope.$watch('hasFieldsDefinitions', function(newVal) {
                                if (newVal !== true) return;
                                $scope.retrieveData();
                            });

                            $scope.bindFields = function() {
                                angular.forEach($scope.fields, function(field) {
                                    field.$parent.Model = $scope.model;

                                    if (field.$editorType == 'input' &&
                                        angular.equals(field.value, $scope.model[field.Name]) == false) {
                                        field.value = (field.DataType == 'date') ? new Date($scope.model[field.Name]) : $scope.model[field.Name];

                                        $scope.$watch(function() {
                                            return field.value;
                                        }, function(value) {
                                            $scope.model[field.Name] = value;
                                        });
                                    }

                                    // Ignores models without state
                                    if (angular.isUndefined($scope.model.$state)) return;

                                    if (angular.equals(field.state, $scope.model.$state[field.Name]) == false) {
                                        field.state = $scope.model.$state[field.Name];
                                    }
                                });
                            };

                            $scope.retrieveData = function() {
                                if (angular.isUndefined($scope.serverUrl)) {
                                    if (angular.isUndefined($scope.model)) {
                                        $scope.model = new TubularModel($scope, {}, $scope.gridDataService);
                                    }

                                    $scope.bindFields();

                                    return;
                                }

                                if (angular.isUndefined($scope.modelKey) || $scope.modelKey == null || $scope.modelKey == '')
                                    return;

                                $scope.gridDataService.getByKey($scope.serverUrl, $scope.modelKey).promise.then(
                                    function(data) {
                                        $scope.model = new TubularModel($scope, data, $scope.gridDataService);
                                        $scope.bindFields();
                                    }, function(error) {
                                        $scope.$emit('tbForm_OnConnectionError', error);
                                    });
                            };

                            $scope.save = function() {
                                $scope.currentRequest = $scope.model.save();

                                if ($scope.currentRequest === false) {
                                    $scope.$emit('tbForm_OnSavingNoChanges', $scope.model);
                                    return;
                                }

                                $scope.currentRequest.then(
                                        function(data) {
                                            $scope.$emit('tbForm_OnSuccessfulSave', data);
                                        }, function(error) {
                                            $scope.$emit('tbForm_OnConnectionError', error);
                                        })
                                    .then(function() {
                                        $scope.model.$isLoading = false;
                                        $scope.currentRequest = null;
                                    });
                            };

                            $scope.update = function() {
                                $scope.save();
                            };

                            $scope.create = function() {
                                $scope.model.$isNew = true;
                                $scope.save();
                            };

                            $scope.cancel = function() {
                                $scope.$emit('tbForm_OnCancel', $scope.model);
                            };
                        }
                    ],
                    compile: function compile(cElement, cAttrs) {
                        return {
                            pre: function(scope, lElement, lAttrs, lController, lTransclude) {},
                            post: function(scope, lElement, lAttrs, lController, lTransclude) {
                                scope.hasFieldsDefinitions = true;
                            }
                        };
                    }
                };
            }
        ]);
})();
///#source 1 1 tubular/tubular-models.js
(function() {
    'use strict';

   /**                                           
    * @ngdoc module
    * @name tubular.models
    * 
    * @description
    * Tubular Models module. 
    * 
    * It contains model's factories to be use in {@link tubular.directives} like `tubularModel` and `tubulargGridColumnModel`.
    */
    angular.module('tubular.models', [])
       /**
        * @ngdoc factory
        * @name tubulargGridColumnModel
        *
        * @description
        * The `tubulargGridColumnModel` factory is the base to generate a column model to use with `tbGrid`.
        * 
        * This model doesn't need to be created in your controller, the `tbGrid` generate it from any `tbColumn`.
        */
        .factory('tubulargGridColumnModel', function() {

            var parseSortDirection = function(value) {
                if (angular.isUndefined(value))
                    return 'None';

                if (value.indexOf('Asc') === 0 || value.indexOf('asc') === 0)
                    return 'Ascending';
                if (value.indexOf('Desc') === 0 || value.indexOf('desc') === 0)
                    return 'Descending';

                return 'None';
            };

            return function(attrs) {
                this.Name = attrs.name || null;
                this.Label = attrs.label || null;
                this.Sortable = attrs.sortable === "true";
                this.SortOrder = parseInt(attrs.sortOrder) || -1;
                this.SortDirection = parseSortDirection(attrs.sortDirection);
                this.IsKey = attrs.isKey === "true";
                this.Searchable = attrs.searchable === "true";
                this.Visible = attrs.visible === "false" ? false : true;
                this.Filter = null;
                this.DataType = attrs.columnType || "string";
                this.IsGrouping = attrs.isGrouping === "true";

                this.FilterOperators = {
                    'string': {
                        'None': 'None',
                        'Equals': 'Equals',
                        'Contains': 'Contains',
                        'StartsWith': 'Starts With',
                        'EndsWith': 'Ends With'
                    },
                    'numeric': {
                        'None': 'None',
                        'Equals': 'Equals',
                        'Between': 'Between',
                        'Gte': '>=',
                        'Gt': '>',
                        'Lte': '<=',
                        'Lt': '<',
                    },
                    'date': {
                        'None': 'None',
                        'Equals': 'Equals',
                        'Between': 'Between',
                        'Gte': '>=',
                        'Gt': '>',
                        'Lte': '<=',
                        'Lt': '<',
                    },
                    'datetime': {
                        'None': 'None',
                        'Equals': 'Equals',
                        'Between': 'Between',
                        'Gte': '>=',
                        'Gt': '>',
                        'Lte': '<=',
                        'Lt': '<',
                    },
                    'boolean': {
                        'None': 'None',
                        'Equals': 'Equals',
                    }
                };
            };
        })
        /**
        * @ngdoc factory
        * @name tubulargGridFilterModel
        *
        * @description
        * The `tubulargGridFilterModel` factory is the base to generate a filter model to use with `tbGrid`.
        * 
        * This model doesn't need to be created in your controller, the `tubularGridFilterService` generate it.
        */
        .factory('tubulargGridFilterModel', function() {

            return function(attrs) {
                this.Text = attrs.text || null;
                this.Argument = attrs.argument || null;
                this.Operator = attrs.operator || 'Contains';
                this.OptionsUrl = attrs.optionsUrl || null;
            };
        })
        /**
        * @ngdoc factory
        * @name tubularModel
        *
        * @description
        * The `tubularModel` factory is the base to generate a row model to use with `tbGrid` and `tbForm`.
        */
        .factory('tubularModel', [
            '$timeout', '$location', function($timeout, $location) {
                return function($scope, data, dataService) {
                    var obj = {
                        $key: "",
                        $count: 0,
                        $addField: function(key, value) {
                            this.$count++;

                            this[key] = value;
                            if (angular.isUndefined(this.$original)) this.$original = {};
                            this.$original[key] = value;

                            if (angular.isUndefined(this.$state)) this.$state = {};
                            this.$state[key] = {
                                $valid: function() {
                                    return this.$errors.length === 0;
                                },
                                $errors: []
                            };

                            $scope.$watch(function() {
                                return obj[key];
                            }, function(newValue, oldValue) {
                                if (newValue == oldValue) return;
                                obj.$hasChanges = obj[key] != obj.$original[key];
                            });
                        }
                    };

                    if (angular.isArray(data) == false) {
                        angular.forEach(Object.keys(data), function(name) {
                            obj.$addField(name, data[name]);
                        });
                    }

                    if (angular.isDefined($scope.columns)) {
                        angular.forEach($scope.columns, function(col, key) {
                            var value = data[key] || data[col.Name];

                            if (angular.isUndefined(value) && data[key] === 0)
                                value = 0;

                            obj.$addField(col.Name, value);

                            if (col.DataType == "date" || col.DataType == "datetime") {
                                var timezone = new Date().toString().match(/([-\+][0-9]+)\s/)[1];
                                timezone = timezone.substr(0, timezone.length - 2) + ':' + timezone.substr(timezone.length - 2, 2);
                                var tempDate = new Date(Date.parse(obj[col.Name] + timezone));

                                if (col.DataType == "date") {
                                    obj[col.Name] = new Date(1900 + tempDate.getYear(), tempDate.getMonth(), tempDate.getDate());
                                } else {
                                    obj[col.Name] = new Date(1900 + tempDate.getYear(), tempDate.getMonth(), tempDate.getDate(), tempDate.getHours(), tempDate.getMinutes(), tempDate.getSeconds(), 0);
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

                    for (var k in obj) {
                        if (obj.hasOwnProperty(k)) {
                            if (k[0] == '$') continue;

                            obj.$state[k] = {
                                $valid: function() {
                                    return this.$errors.length == 0;
                                },
                                $errors: []
                            };
                        }
                    }

                    obj.$valid = function() {
                        for (var k in obj.$state) {
                            if (obj.$state.hasOwnProperty(k)) {
                                var key = k;
                                if (angular.isUndefined(obj.$state[key]) ||
                                    obj.$state[key] == null ||
                                    angular.isUndefined(obj.$state[key].$valid)) continue;

                                if (obj.$state[key].$valid()) continue;

                                return false;
                            }
                        }

                        return true;
                    };

                    // Returns a save promise
                    obj.save = function() {
                        if (angular.isUndefined(dataService) || dataService == null)
                            throw 'Define DataService to your model.';

                        if (angular.isUndefined($scope.serverSaveUrl) || $scope.serverSaveUrl == null)
                            throw 'Define a Save URL.';

                        if (obj.$hasChanges == false) return false;

                        obj.$isLoading = true;

                        if (obj.$isNew) {
                            return dataService.retrieveDataAsync({
                                serverUrl: $scope.serverSaveUrl,
                                requestMethod: $scope.serverSaveMethod,
                                data: obj
                            }).promise;
                        } else {
                            return dataService.saveDataAsync(obj, {
                                serverUrl: $scope.serverSaveUrl,
                                requestMethod: 'PUT'
                            }).promise;
                        }
                    };

                    obj.edit = function() {
                        if (obj.$isEditing && obj.$hasChanges) {
                            obj.save();
                        }

                        obj.$isEditing = !obj.$isEditing;
                    };

                    obj.delete = function() {
                        $scope.deleteRow(obj);
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
                                if (k[0] == '$' || angular.isUndefined(obj.$original[k])) {
                                    continue;
                                }

                                obj[k] = obj.$original[k];
                            }
                        }

                        obj.$isEditing = false;
                        obj.$hasChanges = false;
                    };

                    obj.editForm = function(view) {
                        $location.path(view + "/" + obj.$key);
                    };

                    return obj;
                };
            }
        ]);
})();
///#source 1 1 tubular/tubular-services.js
(function() {
    'use strict';

    /**
     * @ngdoc module
     * @name tubular.services
     * 
     * @description
     * Tubular Services module. 
     * It contains common services like Http and OData clients, and filtering and printing services.
     */
    angular.module('tubular.services', ['ui.bootstrap', 'ngCookies'])
        /**
         * @ngdoc service
         * @name tubularPopupService
         *
         * @description
         * Use `tubularPopupService` to show or generate popups with a `tbForm` inside.
         */
        .service('tubularPopupService', [
            '$modal', '$rootScope', 'tubularTemplateService', function tubularPopupService($modal, $rootScope, tubularTemplateService) {
                var me = this;

                me.onSuccessForm = function(callback) {
                    $rootScope.$on('tbForm_OnSuccessfulSave', callback);
                };

                me.onConnectionError = function(callback) {
                    $rootScope.$on('tbForm_OnConnectionError', callback);
                };

                me.openDialog = function(template, model) {
                    if (angular.isUndefined(template))
                        template = tubularTemplateService.generatePopup(model);

                    var dialog = $modal.open({
                        templateUrl: template,
                        backdropClass: 'fullHeight',
                        controller: [
                            '$scope', function($scope) {
                                $scope.Model = model;

                                $scope.savePopup = function() {
                                    var result = $scope.Model.save();

                                    if (angular.isUndefined(result) || result === false) return;

                                    result.then(
                                        function(data) {
                                            $scope.$emit('tbForm_OnSuccessfulSave', data);
                                            $rootScope.$broadcast('tbForm_OnSuccessfulSave', data);
                                            $scope.Model.$isLoading = false;
                                            dialog.close();
                                        }, function(error) {
                                            $scope.$emit('tbForm_OnConnectionError', error);
                                            $rootScope.$broadcast('tbForm_OnConnectionError', error);
                                            $scope.Model.$isLoading = false;
                                        });
                                };

                                $scope.closePopup = function() {
                                    if (angular.isDefined($scope.Model.revertChanges))
                                        $scope.Model.revertChanges();

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
         * Use `tubularGridExportService` to export your `tbGrid` to CSV format.
         */
        .service('tubularGridExportService', function tubularGridExportService() {
            var me = this;

            me.getColumns = function(gridScope) {
                return gridScope.columns
                    .filter(function(c) { return c.Visible; })
                    .map(function(c) { return c.Name.replace(/([a-z])([A-Z])/g, '$1 $2'); });
            };

            me.getColumnsVisibility = function(gridScope) {
                return gridScope.columns
                    .map(function(c) { return c.Visible; });
            };

            me.exportAllGridToCsv = function(filename, gridScope) {
                var columns = me.getColumns(gridScope);
                var visibility = me.getColumnsVisibility(gridScope);

                gridScope.getFullDataSource(function(data) {
                    me.exportToCsv(filename, columns, data, visibility);
                });
            };

            me.exportGridToCsv = function(filename, gridScope) {
                var columns = me.getColumns(gridScope);
                var visibility = me.getColumnsVisibility(gridScope);

                gridScope.currentRequest = {};
                me.exportToCsv(filename, columns, gridScope.dataSource.Payload, visibility);
                gridScope.currentRequest = null;
            };

            me.exportToCsv = function(filename, header, rows, visibility) {
                var processRow = function(row) {
                    if (typeof (row) === 'object')
                        row = Object.keys(row).map(function(key) { return row[key]; });

                    var finalVal = '';
                    for (var j = 0; j < row.length; j++) {
                        if (visibility[j] === false) continue;
                        var innerValue = row[j] === null ? '' : row[j].toString();
                        if (row[j] instanceof Date) {
                            innerValue = row[j].toLocaleString();
                        }
                        var result = innerValue.replace(/"/g, '""');
                        if (result.search(/("|,|\n)/g) >= 0)
                            result = '"' + result + '"';
                        if (j > 0)
                            finalVal += ',';
                        finalVal += result;
                    }
                    return finalVal + '\n';
                };

                var csvFile = '';

                if (header.length > 0)
                    csvFile += processRow(header);

                for (var i = 0; i < rows.length; i++) {
                    csvFile += processRow(rows[i]);
                }

                // Add "\uFEFF" like UTF-8 BOM
                var blob = new Blob(["\uFEFF" + csvFile], { type: 'text/csv;charset=utf-8;' });
                saveAs(blob, filename);
            };
        })
        /**
         * @ngdoc service
         * @name tubularGridFilterService
         *
         * @description
         * The `tubularGridFilterService` service is a internal helper to setup any `FilterModel` with a UI.
         */
        .service('tubularGridFilterService', [
            'tubulargGridFilterModel', '$compile', '$modal', function tubularGridFilterService(FilterModel, $compile, $modal) {
                var me = this;

                me.applyFilterFuncs = function(scope, el, attributes, openCallback) {
                    scope.columnSelector = attributes.columnSelector || false;
                    scope.$component = scope.$parent.$component;
                    scope.filterTitle = "Filter";

                    scope.clearFilter = function() {
                        scope.filter.Operator = 'None';
                        scope.filter.Text = '';
                        scope.filter.Argument = [];

                        scope.$component.retrieveData();
                        scope.close();
                    };

                    scope.applyFilter = function() {
                        scope.$component.retrieveData();
                        scope.close();
                    };

                    scope.close = function() {
                        $(el).find('[data-toggle="popover"]').popover('hide');
                    };

                    scope.openColumnsSelector = function() {
                        scope.close();

                        var model = scope.$component.columns;

                        var dialog = $modal.open({
                            template: '<div class="modal-header">' +
                                '<h3 class="modal-title">Columns Selector</h3>' +
                                '</div>' +
                                '<div class="modal-body">' +
                                '<table class="table table-bordered table-responsive table-striped table-hover table-condensed">' +
                                '<thead><tr><th>Visible?</th><th>Name</th><th>Is grouping?</th></tr></thead>' +
                                '<tbody><tr ng-repeat="col in Model">' +
                                '<td><input type="checkbox" ng-model="col.Visible" /></td>' +
                                '<td>{{col.Label}}</td>' +
                                '<td><input type="checkbox" ng-disabled="true" ng-model="col.IsGrouping" /></td>' +
                                '</tr></tbody></table></div>' +
                                '</div>' +
                                '<div class="modal-footer"><button class="btn btn-warning" ng-click="closePopup()">Close</button></div>',
                            backdropClass: 'fullHeight',
                            controller: [
                                '$scope', function($innerScope) {
                                    $innerScope.Model = model;

                                    $innerScope.closePopup = function() {
                                        dialog.close();
                                    };
                                }
                            ]
                        });
                    };

                    $(el).find('[data-toggle="popover"]').popover({
                        html: true,
                        content: function() {
                            var selectEl = $(this).next().find('select').find('option').remove().end();
                            angular.forEach(scope.filterOperators, function(val, key) {
                                $(selectEl).append('<option value="' + key + '">' + val + '</option>');
                            });

                            return $compile($(this).next().html())(scope);
                        },
                    });

                    $(el).find('[data-toggle="popover"]').on('shown.bs.popover', openCallback);
                };

                me.createFilterModel = function(scope, lAttrs) {
                    scope.filter = new FilterModel(lAttrs);
                    scope.filter.Name = scope.$parent.column.Name;

                    var columns = scope.$component.columns.filter(function(el) {
                        return el.Name === scope.filter.Name;
                    });

                    if (columns.length === 0) return;

                    columns[0].Filter = scope.filter;
                    scope.dataType = columns[0].DataType;
                    scope.filterOperators = columns[0].FilterOperators[scope.dataType];

                    if (scope.dataType === 'datetime' || scope.dataType === 'date') {
                        scope.filter.Argument = [new Date()];
                        scope.filter.Operator = 'Equals';
                    }

                    if (scope.dataType === 'numeric') {
                        scope.filter.Argument = [1];
                        scope.filter.Operator = 'Equals';
                    }

                    scope.filterTitle = lAttrs.title || "Filter";
                };
            }
        ])
        /**
         * @ngdoc service
         * @name tubularEditorService
         *
         * @description
         * The `tubularEditorService` service is a internal helper to setup any `TubularModel` with a UI.
         */
        .service('tubularEditorService', [
            function tubularEditorService() {
                var me = this;

                me.defaultScope = {
                    value: '=?',
                    state: '=?',
                    isEditing: '=?',
                    editorType: '@',
                    showLabel: '=?',
                    label: '@?',
                    required: '=?',
                    format: '@?',
                    min: '=?',
                    max: '=?',
                    name: '@',
                    defaultValue: '=?',
                    IsKey: '@',
                    placeholder: '@?',
                    readOnly: '=?',
                    help: '@?'
                };

                me.setupScope = function(scope, defaultFormat) {
                    scope.isEditing = angular.isUndefined(scope.isEditing) ? true : scope.isEditing;
                    scope.showLabel = scope.showLabel || false;
                    scope.label = scope.label || (scope.name || '').replace(/([a-z])([A-Z])/g, '$1 $2');
                    scope.required = scope.required || false;
                    scope.readOnly = scope.readOnly || false;
                    scope.format = scope.format || defaultFormat;
                    scope.$valid = true;
                    scope.$editorType = 'input';

                    if (angular.isUndefined(scope.defaultValue) == false && angular.isUndefined(scope.value)) {
                        scope.value = scope.defaultValue;
                    }

                    scope.$watch('value', function(newValue, oldValue) {
                        if (angular.isUndefined(oldValue) && angular.isUndefined(newValue)) return;

                        if (angular.isUndefined(scope.state)) {
                            scope.state = {
                                $valid: function() {
                                    return this.$errors.length === 0;
                                },
                                $errors: []
                            };
                        }

                        scope.$valid = true;
                        scope.state.$errors = [];

                        // Try to match the model to the parent, if it exists
                        if (angular.isDefined(scope.$parent.Model)) {
                            if (angular.isDefined(scope.$parent.Model[scope.name])) {
                                scope.$parent.Model[scope.name] = newValue;
                            } else if (angular.isDefined(scope.$parent.Model.$addField)) {
                                scope.$parent.Model.$addField(scope.name, newValue);
                            }
                        }

                        if (angular.isUndefined(scope.value) && scope.required) {
                            scope.$valid = false;
                            scope.state.$errors = ["Field is required"];
                            return;
                        }

                        // Check if we have a validation function, otherwise return
                        if (angular.isUndefined(scope.validate)) return;

                        scope.validate();
                    });

                    var parent = scope.$parent;

                    // We try to find a Tubular Form in the parents
                    while (true) {
                        if (parent == null) break;
                        if (angular.isUndefined(parent.tubularDirective) == false &&
                            parent.tubularDirective === 'tubular-form') {
                            parent.addField(scope);
                            break;
                        }

                        parent = parent.$parent;
                    }
                };
            }
        ]);
})();
///#source 1 1 tubular/tubular-services-http.js
(function () {
    'use strict';

    angular.module('tubular.services')
        /**
         * @ngdoc service
         * @name tubularHttp
         *
         * @description
         * Use `tubularHttp` to connect a grid or a form to a HTTP Resource. Internally this service is
         * using `$http` to make all the connections.
         * 
         * This service provides authentication using bearer-tokens. Based on https://bitbucket.org/david.antaramian/so-21662778-spa-authentication-example
         */
        .service('tubularHttp', [
        '$http', '$timeout', '$q', '$cacheFactory', '$cookieStore', function tubularHttp($http, $timeout, $q, $cacheFactory, $cookieStore) {
            function isAuthenticationExpired(expirationDate) {
                var now = new Date();
                expirationDate = new Date(expirationDate);

                return expirationDate - now <= 0;
            }

            function saveData() {
                removeData();
                $cookieStore.put('auth_data', me.userData);
            }

            function removeData() {
                $cookieStore.remove('auth_data');
            }

            function retrieveSavedData() {
                var savedData = $cookieStore.get('auth_data');

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

            function setHttpAuthHeader() {
                $http.defaults.headers.common.Authorization = 'Bearer ' + me.userData.bearerToken;
            }

            var me = this;
            me.userData = {
                isAuthenticated: false,
                username: '',
                bearerToken: '',
                expirationDate: null,
            };

            me.cache = $cacheFactory('tubularHttpCache');
            me.useCache = true;
            me.requireAuthentication = true;

            me.isAuthenticated = function() {
                if (!me.userData.isAuthenticated || isAuthenticationExpired(me.userData.expirationDate)) {
                    try {
                        retrieveSavedData();
                    } catch (e) {
                        return false;
                    }
                }

                return true;
            };

            me.setRequireAuthentication = function(val) {
                me.requireAuthentication = val;
            };

            me.removeAuthentication = function() {
                removeData();
                clearUserData();
                $http.defaults.headers.common.Authorization = null;
            };

            me.authenticate = function(username, password, successCallback, errorCallback, persistData) {
                this.removeAuthentication();
                var config = {
                    method: 'POST',
                    url: '/api/token',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                    },
                    data: 'grant_type=password&username=' + username + '&password=' + password,
                };

                $http(config)
                    .success(function(data) {
                        me.userData.isAuthenticated = true;
                        me.userData.username = data.userName;
                        me.userData.bearerToken = data.access_token;
                        me.userData.expirationDate = new Date(data['.expires']);
                        setHttpAuthHeader();
                        if (persistData === true) {
                            saveData();
                        }
                        if (typeof successCallback === 'function') {
                            successCallback();
                        }
                    })
                    .error(function(data) {
                        if (typeof errorCallback === 'function') {
                            if (data.error_description) {
                                errorCallback(data.error_description);
                            } else {
                                errorCallback('Unable to contact server; please, try again later.');
                            }
                        }
                    });
            };

            me.saveDataAsync = function(model, request) {
                var clone = angular.copy(model);
                var originalClone = angular.copy(model.$original);

                delete clone.$isEditing;
                delete clone.$hasChanges;
                delete clone.$selected;
                delete clone.$original;
                delete clone.$state;
                delete clone.$valid;

                request.data = {
                    Old: originalClone,
                    New: clone
                };

                var dataRequest = me.retrieveDataAsync(request);

                dataRequest.promise.then(function(data) {
                    model.$hasChanges = false;
                    model.resetOriginal();
                    return data;
                });

                return dataRequest;
            };

            me.getExpirationDate = function() {
                var date = new Date();
                var minutes = 5;
                return new Date(date.getTime() + minutes * 60000);
            };

            me.checksum = function(obj) {
                var keys = Object.keys(obj).sort();
                var output = [], prop;
                for (var i = 0; i < keys.length; i++) {
                    prop = keys[i];
                    output.push(prop);
                    output.push(obj[prop]);
                }
                return JSON.stringify(output);
            };

            me.retrieveDataAsync = function(request) {
                var canceller = $q.defer();

                var cancel = function(reason) {
                    console.error(reason);
                    canceller.resolve(reason);
                };

                if (angular.isString(request.requireAuthentication)) {
                    request.requireAuthentication = request.requireAuthentication == "true";
                } else {
                    request.requireAuthentication = request.requireAuthentication || me.requireAuthentication;
                }

                if (request.requireAuthentication && me.isAuthenticated() == false) {
                    // Return empty dataset
                    return {
                        promise: $q(function(resolve, reject) {
                            resolve(null);
                        }),
                        cancel: cancel
                    };
                }

                var checksum = me.checksum(request);

                if ((request.requestMethod == 'GET' || request.requestMethod == 'POST') && me.useCache) {
                    var data = me.cache.get(checksum);

                    if (angular.isDefined(data) && data.Expiration.getTime() > new Date().getTime()) {
                        return {
                            promise: $q(function(resolve, reject) {
                                resolve(data.Set);
                            }),
                            cancel: cancel
                        };
                    }
                }

                var promise = $http({
                    url: request.serverUrl,
                    method: request.requestMethod,
                    data: request.data,
                    timeout: canceller.promise
                }).then(function(response) {
                    $timeout.cancel(timeoutHanlder);

                    if (me.useCache) {
                        me.cache.put(checksum, { Expiration: me.getExpirationDate(), Set: response.data });
                    }

                    return response.data;
                }, function (error) {
                    if (angular.isDefined(error) && angular.isDefined(error.status) && error.status == 401) {
                        if (me.isAuthenticated()) {
                            me.removeAuthentication();
                            // Let's trigger a refresh
                            document.location = document.location;
                        }
                    }

                    return $q.reject(error);
                });

                request.timeout = request.timeout || 15000;

                var timeoutHanlder = $timeout(function() {
                    cancel('Timed out');
                }, request.timeout);

                return {
                    promise: promise,
                    cancel: cancel
                };
            };

            me.get = function (url) {
                if (me.requireAuthentication && me.isAuthenticated() == false) {
                    var canceller = $q.defer();

                    // Return empty dataset
                    return {
                        promise: $q(function (resolve, reject) {
                            resolve(null);
                        }),
                        cancel: function (reason) {
                            console.error(reason);
                            canceller.resolve(reason);
                        }
                    };
                }

                return { promise: $http.get(url).then(function (data) { return data.data; }) };
            };

            me.delete = function(url) {
                return me.retrieveDataAsync({
                    serverUrl: url,
                    requestMethod: 'DELETE',
                });
            };

            me.post = function(url, data) {
                return me.retrieveDataAsync({
                    serverUrl: url,
                    requestMethod: 'POST',
                    data: data
                });
            };

            me.put = function(url, data) {
                return me.retrieveDataAsync({
                    serverUrl: url,
                    requestMethod: 'PUT',
                    data: data
                });
            };

            me.getByKey = function (url, key) {
                return me.get(url + key);
            };
        }
    ]);
})();
///#source 1 1 tubular/tubular-services-odata.js
(function () {
    'use strict';

    angular.module('tubular.services')
        /**
         * @ngdoc service
         * @name tubularOData
         *
         * @description
         * Use `tubularOData` to connect a grid or a form to an OData Resource.
         * 
         * This service provides authentication using bearer-tokens.
         */
        .service('tubularOData', ['tubularHttp', function tubularOData(tubularHttp) {
                var me = this;

                me.requireAuthentication = true;

                me.setRequireAuthentication = function (val) {
                    me.requireAuthentication = val;
                };

                // {0} represents column name and {1} represents filter value
                me.operatorsMapping = {
                    'None': '',
                    'Equals': "{0} eq {1}",
                    'Contains': "substringof({1}, {0}) eq true",
                    'StartsWith': "startswith({0}, {1}) eq true",
                    'EndsWith': "endswith({0}, {1}) eq true",
                    // TODO: 'Between': 'Between', 
                    'Gte': "{0} ge {1}",
                    'Gt': "{0} gt {1}",
                    'Lte': "{0} le {1}",
                    'Lt': "{0} lt {1}",
                };

                me.retrieveDataAsync = function(request) {
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
                        .filter(function(el) { return el.SortOrder > 0; })
                        .sort(function(a, b) { return a.SortOrder - b.SortOrder; })
                        .map(function(el) { return el.Name + " " + (el.SortDirection == "Descending" ? "desc" : ""); });

                    if (order.length > 0)
                        url += "&$orderby=" + order.join(',');

                    var filter = params.Columns
                        .filter(function(el) { return el.Filter != null && el.Filter.Text != null; })
                        .map(function(el) {
                            return me.operatorsMapping[el.Filter.Operator]
                                .replace('{0}', el.Name)
                                .replace('{1}', el.DataType == "string" ? "'" + el.Filter.Text + "'" : el.Filter.Text);
                        })
                        .filter(function(el) { return el.length > 1; });


                    if (params.Search != null && params.Search.Operator == 'Auto') {
                        var freetext = params.Columns
                            .filter(function(el) { return el.Searchable; })
                            .map(function(el) {
                                return "startswith({0}, '{1}') eq true".replace('{0}', el.Name).replace('{1}', params.Search.Text);
                            });

                        if (freetext.length > 0)
                            filter.push("(" + freetext.join(' or ') + ")");
                    }

                    if (filter.length > 0)
                        url += "&$filter=" + filter.join(' and ');

                    request.data = null;
                    request.serverUrl = url;

                    tubularHttp.setRequireAuthentication(request.requireAuthentication || me.requireAuthentication);

                    var response = tubularHttp.retrieveDataAsync(request);

                    var promise = response.promise.then(function(data) {
                        var result = {
                            Payload: data.value,
                            CurrentPage: 1,
                            TotalPages: 1,
                            TotalRecordCount: 1,
                            FilteredRecordCount: 1
                        };

                        result.TotalRecordCount = data["odata.count"];
                        result.FilteredRecordCount = result.TotalRecordCount; // TODO: Calculate filtered items
                        result.TotalPages = parseInt(result.TotalRecordCount / params.Take);
                        result.CurrentPage = parseInt(1 + ((params.Skip / result.FilteredRecordCount) * result.TotalPages));

                        return result;
                    });

                    return {
                        promise: promise,
                        cancel: response.cancel
                    };
                };

                me.saveDataAsync = function (model, request) {
                    tubularHttp.setRequireAuthentication(request.requireAuthentication || me.requireAuthentication);
                    return tubularHttp.saveDataAsync(model, request); //TODO: Check how to handle
                };

                me.get = function(url) {
                    return tubularHttp.get(url);
                };

                me.delete = function(url) {
                    return tubularHttp.delete(url);
                };

                me.post = function(url, data) {
                    return tubularHttp.post(url, data);
                };

                me.put = function(url, data) {
                    return tubularHttp.put(url, data);
                };

                me.getByKey = function (url, key) {
                    tubularHttp.setRequireAuthentication(me.requireAuthentication);
                    return tubularHttp.get(url + "(" + key + ")");
                };
            }
        ]);
})();
///#source 1 1 tubular/tubular-services-template.js
(function() {
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
        .service('tubularTemplateService', [
            '$templateCache',
            function tubularTemplateService($templateCache) {
                var me = this;

                me.enums = tubularTemplateServiceModule.enums;
                me.defaults = tubularTemplateServiceModule.defaults;

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
})();
///#source 1 1 tadaaapickr/tadaaapickr.pack.js
/**
 * Usage example
 * var
 */
(function define(namespace) {

    var validParts = /dd?|mm?|MM(?:M)?|yy(?:yy)?/g;

    /**
	 * Adds n units of time to date d
	 * @param d:{Date}
	 * @param n:{Number} (can be negative)
	 * @param unit:{String} Accepted values are only : d|days, m|months, y|years
	 * @return {Date}
	 */
    function addToDate(d, n, unit) {
        var unitCode = unit.charAt(0);
        if (unitCode == "d") {
            return new Date(d.getFullYear(), d.getMonth(), d.getDate() + n);
        } else if (unitCode == "m") {
            return new Date(d.getFullYear(), d.getMonth() + n, d.getDate());
        } else if (unitCode == "y") {
            return new Date(d.getFullYear() + n, d.getMonth(), d.getDate());
        }
    }

    /**
	 * Get the difference (duration) between two dates/times in one of the following units :
	 * 'd|days', 'm|months', 'y|years'
	 */
    function elapsed(unit, d1, d2) {
        var unitCode = unit.charAt(0);
        if (unitCode == "d") {
            return Math.round((d2 - d1) / 86400000); // 1000*60*60*24ms
        } else if (unitCode == "m") {
            return (d1.getFullYear() + d1.getMonth() * 12 - d2.getFullYear() + d2.getMonth() * 12) / 12;
        } else if (unitCode == "y") {
            return (d1.getFullYear() - d2.getFullYear());
        }
    };

    /**
	 * Decompose a format string into its separators and date parts
	 * @param fmt
	 * @return {Object}
	 */
    function parseFormat(fmt) {
        // IE treats \0 as a string end in inputs (truncating the value),
        // so it's a bad format delimiter, anyway
        var parts = fmt.match(validParts),
			separators = fmt.replace(validParts, '\0').split('\0');

        if (!separators || !separators.length || !parts || parts.length == 0) {
            throw new Error("Invalid date format : " + fmt);
        }

        var positions = {};

        for (var i = 0, len = parts.length; i < len; i++) {
            var letter = parts[i].substr(0, 1).toUpperCase();
            positions[letter] = i;
        }

        return { separators: separators, parts: parts, positions: positions };
    }

    /**
	 * Returns a component of a formated date
	 * @param d
	 * @param partName
	 * @param loc
	 * @return {*}
	 */
    function dateParts(d, partName, loc) {

        switch (partName) {
            case 'dd': return (100 + d.getDate()).toString().substring(1);
            case 'mm': return (100 + d.getMonth() + 1).toString().substring(1);
            case 'yyyy': return d.getFullYear();
            case 'yy': return d.getFullYear() % 100;

            case 'MM': return Date.locales[loc].monthsShort[d.getMonth()];
            case 'MMM': return Date.locales[loc].months[d.getMonth()];

            case 'd': return d.getDate();
            case 'm': return (d.getMonth() + 1);
        }
    }

    /**
	 * Format a given date according to the specified format
	 * @param d
	 * @param fmt a format string or a parsed format
	 * @return {String}
	 */
    function formatDate(d, fmt, loc) {

        if (!d || isNaN(d)) return "";

        var date = [],
			format = (typeof (fmt) == "string") ? parseFormat(fmt) : fmt,
			seps = format.separators;

        for (var i = 0, len = format.parts.length; i < len; i++) {
            if (seps[i]) date.push(seps[i]);
            date.push(dateParts(d, format.parts[i], loc));
        }
        return date.join('');
    }

    function parseDate(str, fmt) {

        if (!str) return undefined;

        var format = (typeof (fmt) == "string") ? parseFormat(fmt) : fmt,
			matches = str.match(/[0-9]+/g); // only number parts interest us..

        if (matches && matches.length == 3) {
            var positions = format.positions; // tells us where the year, month and day are located
            return new Date(
				matches[positions.Y],
				matches[positions.M] - 1,
				matches[positions.D]
			);

        } else { // fall back on the Date constructor that can parse ISO8601 and other (english) formats..
            var parsed = new Date(str);
            return (isNaN(parsed.getTime()) ? undefined : parsed);
        }

    }


    var exportables = {
        add: addToDate,
        elapsed: elapsed,
        parseFormat: parseFormat,
        format: formatDate,
        parse: parseDate,
        locales: {
            en: {
                days: ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"],
                daysShort: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
                daysMin: ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"],
                months: ["January", "February", "March", "April", "May", "June",
							 "July", "August", "September", "October", "November", "December"],
                monthsShort: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
            }
        }
    };


    // Temporary export under the 'Date' namespace in the browser
    for (var methodName in exportables) {
        namespace[methodName] = exportables[methodName];
    }

})(this.module ? this.module.exports : Date);
/*
 A lightweight/nofuzz/bootstraped/pwned DatePicker for jQuery 1.7..
 that has built-in internationalization support,
 keyboard accessibility the full way,
 and very fast rendering
 - Compatible with a subset of the jquery UI Date Picker
 - Styled with Bootstrap
 Complete project source available at:
 https://github.com/zipang/tadaaapickr/
 Copyright (c) 2012 Christophe Desguez.  All rights reserved.
 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:
 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.
 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
 */
(function ($) {

    var defaults = {
        calId: "datepicker",
        dateFormat: "mm/dd/yyyy",
        language: "en",
        firstDayOfWeek: 0, // the only choices are : 0 = Sunday, 1 = Monday,
        required: false
    };

    /**
	 * This Constructor is publicly exposed as $.fn.datepicker.Calendar
	 * @param $target the input element to bind on
	 * @param options
	 */
    var Calendar = function ($target, options) {
        this._init(this.$target = $target, this.settings = options);
    };

    Calendar.prototype = {

        _init: function ($target, options) {

            var loc = options.locale;
            if (loc) { // retrieve the defaults options associated with this locale
                var locale = Calendar.locales[loc];
                $.extend(options, { language: loc }, locale.defaults);
            }

            // Retrieve or reuse the calendar widget
            // If more than one calendar must be displayed at the same time, different calIDs must be provided
            this.$cal = Calendar.build(options.calId, options.language);

            this.setDateFormat(options.format || options.dateFormat)
				.setStartDate(options.startDate)
				.setEndDate(options.endDate);

            this.firstDayOfWeek = options.firstDayOfWeek;
            this.locale = Calendar.getLocale(options.language);
            this.defaultDate = (options.defaultDate || today()); // what to display on first appearance ?

            // Retrieve the current input value and reformat it
            this.setDate(Date.parse($target.val(), this.parsedFormat));

            // Bind all the required event handlers on the input element
            var show = $.proxy(this.show, this);
            $target.data("calendar", this)
				.click(show).focus(show)
				.keydown($.proxy(this.keyHandler, this)).blur($.proxy(this.validate, this));
        },

        _parse: function (d) {
            if (!d) return undefined;
            if (typeof d == "string") return Date.parse(d, this.parsedFormat);
            return atmidnight(d);
        },

        // Show a calendar displaying the current input value
        show: function (e) {
            nope(e);

            var $cal = this.$cal, $target = this.$target;

            if (this.$target.data("dirty")) return; // focus event due to our field update

            if ($cal.hasClass("active")) {
                if ($cal.data("calendar") === this) {
                    return; // already active for this input
                }
                Calendar.hide($cal);
            }

            var targetPos = $target.offset(),
				inputDate = this._parse($target.val());

            this.setDate(inputDate)
				.refreshDays() // coming from another input needs us to refresh the day headers
				.refresh().select();
            this.$cal.css({ left: targetPos.left, top: targetPos.top + $target.outerHeight(false) })
				.slideDown(200).addClass("active").data("calendar", this);

            // active key handler
            this._keyHandler = this.activeKeyHandler;
        },

        hide: function () {
            Calendar.hide(this.$cal);
            this._keyHandler = this.inactiveKeyHandler;
            return this;
        },

        /**
		 * Render the column headers for the days in the proper localization
		 * @param loc
		 * @param firstDayOfWeek (0 : Sunday, 1 : Monday)
		 */
        refreshDays: function () {

            var dayHeaders = this.locale.daysMin,
				firstDayOfWeek = this.firstDayOfWeek;

            // Fill the day's names
            this.$cal.data("$dayHeaders").each(function (i, th) {
                $(th).text(dayHeaders[i + firstDayOfWeek]);
            });

            return this;
        },

        // Refresh (update) the calendar display to reflect the current
        // date selection and locales. If no selection, display the current month
        refresh: function () {

            var d = new Date(this.displayedDate.getTime()),
				displayedMonth = yyyymm(this.displayedDate),
				$cal = this.$cal, $days = $cal.data("$days");

            // refresh month in header
            $cal.data("$header").text(Date.format(d, "MMM yyyy", this.settings.language));

            // find the first date to display
            while (d.getDay() != this.firstDayOfWeek) {
                d = Date.add(d, -1, "day");
            }

            // Calculate cell index of the important dates
            var dday = this.selectedIndex = (this.selectedDate ? Date.elapsed("days", d, this.selectedDate) : undefined),
				startIndex = (this.startDate ? Date.elapsed("days", d, this.startDate) : -Infinity),
				endIndex = (this.endDate ? Date.elapsed("days", d, this.endDate) : +Infinity);


            for (var i = 0; i < 6 * 7; i++) {
                var month = yyyymm(d), dayCell = $days[i], className = "day";
                dayCell.innerHTML = d.getDate();

                if (month < displayedMonth) {
                    className += " old";

                } else if (month > displayedMonth) {
                    className += " new";

                } else if (i == dday) {
                    className += " active";
                }
                if (i < startIndex || i > endIndex) {
                    className += " disabled";
                }
                dayCell.className = className;
                d = Date.add(d, 1, "day");
            }

            return this;
        },

        // Move the displayed date display from specified offset
        // When fantomMove is TRUE, don't update the selected date
        navigate: function (offset, unit, fantomMove) {

            // Cancel the first move when no date was selected : the default date will be displayed instead
            if (!fantomMove && !this.selectedDate) offset = 0;

            var newDate = Date.add((fantomMove ? this.displayedDate : this.selectedDate || this.defaultDate), offset, unit),
				$days = this.$cal.data("$days");

            // Check that we do not pass the boundaries if they are set
            if ((this.startDate && yyyymm(newDate) < yyyymm(this.startDate)) ||
				(this.endDate && yyyymm(newDate) > yyyymm(this.endDate))) {
                return this.select();
            }

            if (yyyymm(newDate) != yyyymm(this.displayedDate) || !this.selectedIndex) {
                if (fantomMove) {
                    this.displayedDate = newDate;
                } else {
                    this.setDate(newDate);
                }
                this.refresh(); // full calendar display refresh needed

            } else {
                // we stay in the same month display : just refresh the 'active' cell
                $($days[this.selectedIndex]).removeClass("active");
                $days[this.selectedIndex += offset].className += " active";
                this.setDate(newDate);
            }

            this.select();
            return false; // WARNING !! : Dirty Hack here to prevent arrow's navigation to deselect date input.
            // We should return 'this' instead to be consistant and chainable, but the code in activeKeyHandler
            // would be less optimized
        },

        select: function () {
            this.$target.data("dirty", true).select().data("dirty", false);
            return this;
        },

        // Set a new start date
        setStartDate: function (d) {
            this.startDate = this._parse(d);
            return this;
        },

        // Set a new end date
        setEndDate: function (d) {
            this.endDate = this._parse(d);
            return this;
        },

        // Set a new selected date
        // When no date is passed, retrieve the input element's val and try to parse it
        setDate: function (d) {

            if (this._parse(d)) {
                this.selectedDate = d;
                this.displayedDate = new Date(d); // don't share the same date instance !

                this.$target.data("date", d).val(Date.format(d, this.parsedFormat));
            } else {
                this.selectedDate = this.selectedIndex = null;
                this.displayedDate = new Date(this.defaultDate);

                this.$target.data("date", null).val("");
            }
            this.displayedDate.setDate(1);
            this.dirty = false;
            return this;
        },

        // Set a new date format
        setDateFormat: function (format) {
            this.parsedFormat = Date.parseFormat(this.dateFormat = format);
            return this;
        },

        // ====== EVENT HANDLERS ====== //

        // the only registred key handler (wrap the call to active or inactive key handler)
        keyHandler: function (e) {
            return this._keyHandler(e);
        },

        // Keyboard navigation when the calendar is active
        activeKeyHandler: function (e) {

            switch (e.keyCode) {

                case 37: // LEFT
                    return (e.ctrlKey) ? this.navigate(-1, "month") : this.navigate(-1, "day");

                case 38: // UP
                    return (e.ctrlKey) ? this.navigate(-1, "year") : this.navigate(-7, "days");

                case 39: // RIGHT
                    return (e.ctrlKey) ? this.navigate(+1, "month") : this.navigate(+1, "day");

                case 40: // DOWN
                    return (e.ctrlKey) ? this.navigate(+1, "year") : this.navigate(+7, "days");

                case 33: // PG-UP
                    return (e.ctrlKey) ? this.navigate(-10, "years") : this.navigate(-1, "year");

                case 34: // PG-DOWN
                    return (e.ctrlKey) ? this.navigate(+10, "years") : this.navigate(+1, "year");

                case 35: // END
                    return this.navigate(+1, "month");

                case 36: // HOME
                    return this.navigate(-1, "month");

                case 9:  // TAB
                case 13: // ENTER
                    // Send the 'Date change' event
                    this.$target.trigger({ type: "dateChange", date: this.selectedDate });
                    return this.hide();

                case 27: // ESC
                    return this.hide();
            }

            // Others keys are sign of a manual input
            this.dirty = true;
        },

        // Key handler when the calendar is not shown
        inactiveKeyHandler: function (e) {

            if (e.keyCode < 41 && e.keyCode > 32) { // Arrows keys > make the calendar reappear
                this.show(e);
                this._keyHandler = this.activeKeyHandler;

            } else {
                // Others keys are sign of a manual input
                this.dirty = true;
            }
        },

        // As manual input is also possible, check date validity on blur (lost focus)
        validate: function (e) {

            if (!this.dirty) return;

            var $target = this.$target, newDate = this._parse($target.val());

            if (!newDate) { // invalid or empty input
                // restore the precedent value or erase the bad input
                this.setDate(this.required ? this.selectedDate || this.defaultDate : null);

            } else if (newDate - this.selectedDate) { // date has changed
                if (newDate < this.startDate || newDate > this.endDate) { // forbidden range
                    this.setDate(this.selectedDate); //restore previous value
                } else { // ok
                    this.setDate(newDate);
                    $target.trigger({ type: "dateChange", date: this.selectedDate });
                }
            }

            this.hide();
        }
    };

    // Calendar (empty) HTML template
    Calendar.template = "<table class='table-condensed'><thead>" // calendar headers include the month and day names
		+ "<tr><th class='prev month'>&laquo;</th><th class='month name' colspan='5'></th><th class='next month'>&raquo;</th></tr>"
		+ "<tr>" + repeat("<th class='dow'/>", 7) + "</tr>"
		+ "</thead><tbody>" // now comes 6 * 7 days
		+ repeat("<tr>" + repeat("<td class='day'/>", 7) + "</tr>", 6)
		+ "</tbody></table>";


    /**
	 * Build a specific Calendar HTML widget with the provided id
	 * and the specific localization. Attach the events
	 */
    Calendar.build = function (calId, loc, firstDayOfWeek) {

        var $cal = $("#" + calId);

        if ($cal.length == 1) {
            return $cal; // reuse an existing widget
        }

        $cal = $("<div>")
			.attr("id", calId)
			.addClass("datepicker dropdown-menu")
			.html(Calendar.template)
			.appendTo("body");

        // Keep a reference on the cells to update
        $cal.data("$days", $("td.day", $cal));
        $cal.data("$header", $("th.month.name", $cal));
        $cal.data("$dayHeaders", $("th.dow", $cal));

        // Define the event handlers
        $cal.on("click", "td.day", function (e) {
            nope(e); // IMPORTANT: prevent the input to loose focus!

            var cal = $cal.data("calendar"),
				$day = $(this), day = +$day.text(),
				firstDayOfMonth = cal.displayedDate,
				monthOffset = ($day.hasClass("old") ? -1 : ($day.hasClass("new") ? +1 : 0)),
				newDate = new Date(firstDayOfMonth.getFullYear(), firstDayOfMonth.getMonth() + monthOffset, day);

            if (newDate < cal.startDate || newDate > cal.endDate) return;

            // Update the $input control
            cal.setDate(newDate).select();

            // Send the event asynchronously
            setTimeout(function () {
                cal.$target.trigger({ type: "dateChange", date: newDate });
                cal.hide();
            }, 0);

        });

        $cal.on("click", "th.month", function (e) {
            nope(e); // IMPORTANT: prevent the input to loose focus!

            var cal = $cal.data("calendar");

            if ($(this).hasClass("prev")) {
                cal.navigate(-1, "month", true);
            } else if ($(this).hasClass("next")) {
                cal.navigate(+1, "month", true);
            }
        });

        return $cal;
    };

    /**
	 * Set all the defaults options associated to a defined locale
	 * @param loc i18n 2 letters country code
	 */
    Calendar.setDefaultLocale = function (loc) {

        var locale = Calendar.locales[loc];

        if (locale) {
            Calendar.setDefaults($.extend({ language: loc }, locale.defaults));
        }
    };

    /**
	 * Return the locales options if they exist, or the english default locale
	 * @param loc a 2 letters i18n language code
	 * @return {*}
	 */
    Calendar.getLocale = function (loc) {
        return (Calendar.locales[loc] || Calendar.locales["en"]);
    };

    /**
	 * Override some predefined defaults with others..
	 * @param options
	 */
    Calendar.setDefaults = function (options) {
        $.extend(defaults, options);
    };

    /**
	 * Hide any instance of any active calendar widget (they should be only one)
	 * Usage calls may be :
	 * Calendar.hide() Hide every active calendar instance
	 * Calendar.hide(evt) (as in document.click)
	 * Calendar.hide($cal) Hide a specific calendar
	 */
    Calendar.hide = function ($cal) {
        var $target = ((!$cal || $cal.originalEvent) ? $(".datepicker.active") : $cal);
        $target.removeClass("active").removeAttr("style");
    };

    // Every other clicks must hide the calendars
    $(document).bind("click", Calendar.hide);

    // Plugin entry
    $.fn.datepicker = function (arg) {

        if (!arg || typeof (arg) === "object") { // initial call to create the calendar

            return $(this).each(function (i, target) {
                var options = $.extend({}, defaults, arg),
					cal = new Calendar($(target), options);
                $(target).data("datepicker", cal);
            });

        } else if (Calendar.prototype[arg]) { // invoke a calendar method on an existing instance

            var methodName = arg, args = Array.prototype.slice.call(arguments, 1);
            return $(this).each(function (i, target) {
                var cal = $(target).data("datepicker");
                try {
                    cal[methodName].apply(cal, args);
                } catch (err) {

                }
            });

        } else {
            $.error("Method " + arg + " does not exist on jquery.datepicker");
        }

    };

    $.fn.datepicker.Calendar = Calendar;

    $.fn.datepicker.Calendar.locales = Date.locales || {
        en: {
            days: ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"],
            daysShort: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
            daysMin: ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"],
            months: ["January", "February", "March", "April", "May", "June",
			             "July", "August", "September", "October", "November", "December"],
            monthsShort: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
        }
    };

    // DATE UTILITIES
    Date.prototype.atMidnight = function () { this.setHours(0, 0, 0, 0); return this; }
    function atmidnight(d) { return (d ? new Date(d.atMidnight()) : undefined); }
    function today() { return (new Date()).atMidnight(); }
    function yyyymm(d) { return d.getFullYear() * 100 + d.getMonth(); }

    function nope(e) { e.stopPropagation(); e.preventDefault(); }

    function repeat(str, n) { return (n == 0) ? "" : Array(n + 1).join(str); }

})(jQuery);
