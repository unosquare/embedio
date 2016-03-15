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
(function() {
    'use strict';

    /**
     * @ngdoc module
     * @name tubular
     * @version 0.9.17
     * 
     * @description 
     * Tubular module. Entry point to get all the Tubular functionality.
     * 
     * It depends upon  {@link tubular.directives}, {@link tubular.services} and {@link tubular.models}.
     */
    angular.module('tubular', ['tubular.directives', 'tubular.services', 'tubular.models', 'LocalStorageModule', 'a8m.group-by'])
        .config([
            'localStorageServiceProvider', function(localStorageServiceProvider) {
                localStorageServiceProvider.setPrefix('tubular');

                // define console methods if not defined
                if (typeof console === "undefined") {
                    window.console = {
                        log: function() {},
                        debug: function() {},
                        error: function() {},
                        assert: function() {},
                        info: function() {},
                        warn: function() {},
                    };
                }
            }
        ])
        .run(['tubularHttp', 'tubularOData', 'tubularLocalData',
            function (tubularHttp, tubularOData, tubularLocalData) {
                // register data services
                tubularHttp.registerService('odata', tubularOData);
                tubularHttp.registerService('local', tubularLocalData);
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
        .filter('errormessage', function() {
            return function(input) {
                if (angular.isDefined(input) && angular.isDefined(input.data) &&
                    input.data &&
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
            '$filter', function($filter) {
                return function(input, format, symbol, fractionSize) {
                    symbol = symbol || "$";
                    fractionSize = fractionSize || 2;

                    if (format === 'C') {
                        return $filter('currency')(input, symbol, fractionSize);
                    }

                    if (format === 'I') {
                        return parseInt(input);
                    }

                    // default to decimal
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
        .filter('characters', function() {
            return function(input, chars, breakOnWord) {
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
(function() {
    'use strict';

    /**
     * @ngdoc module
     * @name tubular.directives
     * 
     * @description 
     * Tubular Directives module. It contains all the directives.
     * 
     * It depends upon {@link tubular.services} and {@link tubular.models}.
     */
    angular.module('tubular.directives', ['tubular.services', 'tubular.models'])
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
                        serverDeleteUrl: '@',
                        serverSaveMethod: '@',
                        pageSize: '@?',
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
                            $scope.name = $scope.name || 'tbgrid';
                            $scope.tubularDirective = 'tubular-grid';
                            $scope.columns = [];
                            $scope.rows = [];

                            $scope.savePage = angular.isUndefined($scope.savePage) ? true : $scope.savePage;
                            $scope.currentPage = $scope.savePage ? (localStorageService.get($scope.name + "_page") || 1) : 1;

                            $scope.savePageSize = angular.isUndefined($scope.savePageSize) ? true : $scope.savePageSize;
                            $scope.pageSize = angular.isUndefined($scope.pageSize) ? 20 : $scope.pageSize;
                            $scope.saveSearch = angular.isUndefined($scope.saveSearch) ? true : $scope.saveSearch;
                            $scope.totalPages = 0;
                            $scope.totalRecordCount = 0;
                            $scope.filteredRecordCount = 0;
                            $scope.requestedPage = $scope.currentPage;
                            $scope.hasColumnsDefinitions = false;
                            $scope.requestCounter = 0;
                            $scope.requestMethod = $scope.requestMethod || 'POST';
                            $scope.serverSaveMethod = $scope.serverSaveMethod || 'POST';
                            $scope.requestTimeout = 15000;
                            $scope.currentRequest = null;
                            $scope.autoSearch = $routeParams.param || ($scope.saveSearch ? (localStorageService.get($scope.name + "_search") || '') : '');
                            $scope.search = {
                                Text: $scope.autoSearch,
                                Operator: $scope.autoSearch == '' ? 'None' : 'Auto'
                            };

                            $scope.isEmpty = false;
                            $scope.tempRow = new TubularModel($scope, {});
                            $scope.dataService = tubularHttp.getDataService($scope.dataServiceName);
                            $scope.requireAuthentication = $scope.requireAuthentication || true;
                            tubularHttp.setRequireAuthentication($scope.requireAuthentication);
                            $scope.editorMode = $scope.editorMode || 'none';
                            $scope.canSaveState = false;
                            $scope.groupBy = '';
                            $scope.showLoading = angular.isUndefined($scope.showLoading) ? true : $scope.showLoading;
                            $scope.autoRefresh = angular.isUndefined($scope.autoRefresh) ? true : $scope.autoRefresh;
                            $scope.serverDeleteUrl = $scope.serverDeleteUrl || $scope.serverSaveUrl;

                            $scope.$watch('columns', function() {
                                if ($scope.hasColumnsDefinitions === false || $scope.canSaveState === false) {
                                    return;
                                }

                                localStorageService.set($scope.name + "_columns", $scope.columns);
                            }, true);

                            $scope.$watch('serverUrl', function(newVal, prevVal) {
                                if ($scope.hasColumnsDefinitions === false || $scope.currentRequest || newVal === prevVal) {
                                    return;
                                }

                                $scope.retrieveData();
                            }, true);

                            $scope.saveSearch = function() {
                                if ($scope.saveSearch) {
                                    if ($scope.search.Text === '') {
                                        localStorageService.remove($scope.name + "_search");
                                    } else {
                                        localStorageService.set($scope.name + "_search", $scope.search.Text);
                                    }
                                }
                            };

                            $scope.addColumn = function(item) {
                                if (item.Name == null) {
                                    return;
                                }

                                if ($scope.hasColumnsDefinitions !== false) {
                                    throw 'Cannot define more columns. Column definitions have been sealed';
                                }

                                $scope.columns.push(item);
                            };

                            $scope.newRow = function(template, popup, size) {
                                $scope.tempRow = new TubularModel($scope, {}, $scope.dataService);
                                $scope.tempRow.$isNew = true;
                                $scope.tempRow.$isEditing = true;
                                $scope.tempRow.$component = $scope;

                                if (angular.isDefined(template)) {
                                    if (angular.isDefined(popup) && popup) {
                                        tubularPopupService.openDialog(template, $scope.tempRow, $scope, size);
                                    }
                                }
                            };

                            $scope.deleteRow = function (row) {
                                var urlparts = $scope.serverDeleteUrl.split('?');
                                var url = urlparts[0] + "/" + row.$key;

                                if (urlparts.length > 1) {
                                    url += '?' + urlparts[1];
                                }

                                var request = {
                                    serverUrl: url,
                                    requestMethod: 'DELETE',
                                    timeout: $scope.requestTimeout,
                                    requireAuthentication: $scope.requireAuthentication,
                                };

                                $scope.currentRequest = $scope.dataService.retrieveDataAsync(request);

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
                                if (columns == null || columns === "") {
                                    // Nothing in settings, saving initial state
                                    localStorageService.set($scope.name + "_columns", $scope.columns);
                                    return;
                                }

                                for (var index in columns) {
                                    if (columns.hasOwnProperty(index)) {
                                        var columnName = columns[index].Name;
                                        var filtered = $scope.columns.filter(function(el) { return el.Name == columnName; });

                                        if (filtered.length === 0) {
                                            continue;
                                        }

                                        var current = filtered[0];
                                        // Updates visibility by now
                                        current.Visible = columns[index].Visible;

                                        // Update sorting
                                        if ($scope.requestCounter < 1) {
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

                            $scope.retrieveData = function() {
                                // If the ServerUrl is empty skip data load
                                if ($scope.serverUrl == '') {
                                    return;
                                }

                                $scope.canSaveState = true;
                                $scope.verifyColumns();

                                if ($scope.savePageSize) {
                                    $scope.pageSize = (localStorageService.get($scope.name + "_pageSize") || $scope.pageSize);
                                }

                                if ($scope.pageSize < 10) $scope.pageSize = 20; // default

                                var skip = ($scope.requestedPage - 1) * $scope.pageSize;

                                if (skip < 0) skip = 0;

                                var request = {
                                    serverUrl: $scope.serverUrl,
                                    requestMethod: $scope.requestMethod || 'POST',
                                    timeout: $scope.requestTimeout,
                                    requireAuthentication: $scope.requireAuthentication,
                                    data: {
                                        Count: $scope.requestCounter,
                                        Columns: $scope.columns,
                                        Skip: skip,
                                        Take: parseInt($scope.pageSize),
                                        Search: $scope.search,
                                        TimezoneOffset: new Date().getTimezoneOffset()
                                    }
                                };

                                if ($scope.currentRequest !== null) {
                                    // This message is annoying when you connect errors to toastr
                                    //$scope.currentRequest.cancel('tubularGrid(' + $scope.$id + '): new request coming.');
                                    return;
                                }

                                if (angular.isUndefined($scope.onBeforeGetData) === false) {
                                    $scope.onBeforeGetData();
                                }

                                $scope.$emit('tbGrid_OnBeforeRequest', request, $scope);

                                $scope.currentRequest = $scope.dataService.retrieveDataAsync(request);

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
                                            var model = new TubularModel($scope, el, $scope.dataService);
                                            model.$component = $scope;

                                            model.editPopup = function(template, size) {
                                                tubularPopupService.openDialog(template, model, $scope, size);
                                            };

                                            return model;
                                        });

                                        $scope.$emit('tbGrid_OnDataLoaded', $scope);

                                        $scope.aggregationFunctions = data.AggregationPayload;
                                        $scope.currentPage = data.CurrentPage;
                                        $scope.totalPages = data.TotalPages;
                                        $scope.totalRecordCount = data.TotalRecordCount;
                                        $scope.filteredRecordCount = data.FilteredRecordCount;
                                        $scope.isEmpty = $scope.filteredRecordCount === 0;

                                        if ($scope.savePage) {
                                            localStorageService.set($scope.name + "_page", $scope.currentPage);
                                        }
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
                                        if (isGrouping) {
                                            throw 'Only one column is allowed to grouping';
                                        }

                                        isGrouping = true;
                                        column.Visible = false;
                                        column.Sortable = true;
                                        column.SortOrder = 1;
                                        $scope.groupBy = column.Name;
                                    }
                                });

                                angular.forEach($scope.columns, function(column) {
                                    if ($scope.groupBy == column.Name) return;

                                    if (column.Sortable && column.SortOrder > 0) {
                                        column.SortOrder++;
                                    }
                                });

                                $scope.retrieveData();
                            });

                            $scope.$watch('pageSize', function() {
                                if ($scope.hasColumnsDefinitions && $scope.requestCounter > 0) {
                                    if ($scope.savePageSize) {
                                        localStorageService.set($scope.name + "_pageSize", $scope.pageSize);
                                    }
                                    $scope.retrieveData();
                                }
                            });

                            $scope.$watch('requestedPage', function() {
                                // TODO: we still need to inter-lock failed, initial and paged requests
                                if ($scope.hasColumnsDefinitions && $scope.requestCounter > 0) {
                                    $scope.retrieveData();
                                }
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
                                if (rows == null || rows === "") {
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
                                $scope.dataService.retrieveDataAsync({
                                    serverUrl: $scope.serverUrl,
                                    requestMethod: $scope.requestMethod || 'POST',
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

                            $scope.visibleColumns = function() {
                                return $scope.columns.filter(function(el) { return el.Visible; }).length;
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
         * @restrict E
         *
         * @description
         * The `tbColumn` directive creates a column in the grid's model. 
         * All the attributes are used to generate a `ColumnModel`.
         * 
         * This directive is replace by a `th` HTML element.
         * 
         * @scope
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
            'tubularGridColumnModel', function (ColumnModel) {
                return {
                    require: '^tbColumnDefinitions',
                    template: '<th ng-transclude ng-class="{sortable: column.Sortable}" ng-show="column.Visible"></th>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: {
                        visible: '=',
                        label: '@?'
                    },
                    controller: [
                        '$scope', function ($scope) {
                            $scope.column = { Label: '' };
                            $scope.$component = $scope.$parent.$parent.$component;
                            $scope.tubularDirective = 'tubular-column';

                            $scope.sortColumn = function (multiple) {
                                $scope.$component.sortColumn($scope.column.Name, multiple);
                            };

                            $scope.$watch("visible", function (val) {
                                if (angular.isDefined(val)) {
                                    $scope.column.Visible = val;
                                }
                            });

                            $scope.$watch('label', function () {
                                $scope.column.Label = $scope.label;
                                // this broadcast here is used for backwards compatibility with tbColumnHeader requiring a scope.label value on its own
                                $scope.$broadcast('tbColumn_LabelChanged', $scope.label);
                            })
                        }
                    ],
                    compile: function compile() {
                        return {
                            pre: function (scope, lElement, lAttrs) {
                                lAttrs.label = angular.isDefined(lAttrs.label) ? lAttrs.label : (lAttrs.name || '').replace(/([a-z])([A-Z])/g, '$1 $2');

                                var column = new ColumnModel(lAttrs);
                                scope.$component.addColumn(column);
                                scope.column = column;
                                scope.label = column.Label;
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
            '$compile', function ($compile) {

                return {
                    require: '^tbColumn',
                    template: '<span><a title="Click to sort. Press Ctrl to sort by multiple columns" class="column-header" href ng-click="sortColumn($event)">' +
                                '<span class="column-header-default">{{ $parent.column.Label }}</span>' +
                                '<span ng-transclude></span></a> ' +
                                '<i class="fa sort-icon" ng-class="' + "{'fa-long-arrow-up': $parent.column.SortDirection == 'Ascending', 'fa-long-arrow-down': $parent.column.SortDirection == 'Descending'}" + '">&nbsp;</i>' +
                                '</span>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false,
                    controller: [
                        '$scope', function ($scope) {
                            $scope.sortColumn = function ($event) {
                                $scope.$parent.sortColumn($event.ctrlKey);
                            };
                            // this listener here is used for backwards compatibility with tbColumnHeader requiring a scope.label value on its own
                            $scope.$on('tbColumn_LabelChanged', function ($event, value) {
                                $scope.label = value;
                            })
                        }
                    ],
                    link: function ($scope, $element, $attrs, controller) {
                        if ($element.find('[ng-transclude] *').length > 0) {
                            $element.find('span.column-header-default').remove();
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
            function () {

                return {
                    require: '^tbGrid',
                    template: '<tfoot ng-transclude></tfoot>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false,
                    controller: [
                        '$scope', function ($scope) {
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
                    // TODO: I can't choose one require: ['^tbRowSet', '^tbFootSet'],
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
                            // TODO: Rename this directive
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
            function() {

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
                        '$scope', function($scope) {
                            $scope.column = { Visible: true };
                            $scope.columnName = $scope.columnName || null;
                            $scope.$component = $scope.$parent.$parent.$component;

                            $scope.getFormScope = function () {
                                // TODO: Implement a form in inline editors
                                return null;
                            };

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
        ]);
})();
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
         * @scope
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
         */
        .directive('tbSimpleEditor', [
            'tubularEditorService', '$filter', function(tubularEditorService, $filter) {

                return {
                    template: '<div ng-class="{ \'form-group\' : showLabel && isEditing, \'has-error\' : !$valid && $dirty() }">' +
                        '<span ng-hide="isEditing">{{value}}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<input type="{{editorType}}" placeholder="{{placeholder}}" ng-show="isEditing" ng-model="value" class="form-control" ' +
                        ' ng-required="required" ng-readonly="readOnly" name="{{name}}" />' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">{{error}}</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: angular.extend({ regex: '@?', regexErrorMessage: '@?' }, tubularEditorService.defaultScope),
                    controller: [
                        '$scope', function($scope) {
                            $scope.validate = function () {
                                if (angular.isDefined($scope.regex) && $scope.regex != null && angular.isDefined($scope.value) && $scope.value != null) {
                                    var patt = new RegExp($scope.regex);

                                    if (patt.test($scope.value) === false) {
                                        $scope.$valid = false;
                                        $scope.state.$errors = [$scope.regexErrorMessage || $filter('translate')('EDITOR_REGEX_DOESNT_MATCH')];
                                        return;
                                    }
                                }

                                if (angular.isDefined($scope.min) && angular.isDefined($scope.value) && $scope.value != null) {
                                    if ($scope.value.length < parseInt($scope.min)) {
                                        $scope.$valid = false;
                                        $scope.state.$errors = [$filter('translate')('EDITOR_MIN_CHARS', $scope.min)];
                                        return;
                                    }
                                }

                                if (angular.isDefined($scope.max) && angular.isDefined($scope.value) && $scope.value != null) {
                                    if ($scope.value.length > parseInt($scope.max)) {
                                        $scope.$valid = false;
                                        $scope.state.$errors = [$filter('translate')('EDITOR_MAX_CHARS', $scope.max)];
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
        .directive('tbNumericEditor', [
            'tubularEditorService', '$filter', function (tubularEditorService, $filter) {

                return {
                    template: '<div ng-class="{ \'form-group\' : showLabel && isEditing, \'has-error\' : !$valid && $dirty() }">' +
                        '<span ng-hide="isEditing">{{value | numberorcurrency: format }}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<div class="input-group" ng-show="isEditing">' +
                        '<div class="input-group-addon" ng-hide="format == \'I\'">{{format == \'C\' ? \'$\' : \'.\'}}</div>' +
                        '<input type="number" placeholder="{{placeholder}}" ng-model="value" class="form-control" ' +
                        'ng-required="required" ng-hide="readOnly" step="{{step || \'any\'}}"  name="{{name}}" />' +
                        '<p class="form-control form-control-static text-right" ng-show="readOnly">{{value | numberorcurrency: format}}</span></p>' +
                        '</div>' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">{{error}}</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: angular.extend({ step: '=?' }, tubularEditorService.defaultScope),
                    controller: [
                        '$scope', function ($scope) {
                            $scope.DataType = "numeric";

                            $scope.validate = function() {
                                if (angular.isDefined($scope.min) && angular.isDefined($scope.value) && $scope.value != null) {
                                    $scope.$valid = $scope.value >= $scope.min;
                                    if (!$scope.$valid) {
                                        $scope.state.$errors = [$filter('translate')('EDITOR_MIN_NUMBER', $scope.min)];
                                    }
                                }

                                if (!$scope.$valid) {
                                    return;
                                }

                                if (angular.isDefined($scope.max) && angular.isDefined($scope.value) && $scope.value != null) {
                                    $scope.$valid = $scope.value <= $scope.max;
                                    if (!$scope.$valid) {
                                        $scope.state.$errors = [$filter('translate')('EDITOR_MAX_NUMBER', $scope.max)];
                                    }
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
        .directive('tbDateTimeEditor', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<div ng-class="{ \'form-group\' : showLabel && isEditing, \'has-error\' : !$valid && $dirty() }">' +
                        '<span ng-hide="isEditing">{{ value | date: format }}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<input type="datetime-local" ng-show="isEditing" ng-model="value" class="form-control" ' +
                        'ng-required="required" ng-readonly="readOnly" name="{{name}}" />' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">' +
                        '{{error}}' +
                        '</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: tubularEditorService.defaultScope,
                    controller: tubularEditorService.dateEditorController('yyyy-MM-dd HH:mm'),
                    compile: function compile() {
                        return {
                            post: function(scope, lElement) {
                                var inp = $(lElement).find("input[type=datetime-local]")[0];
                                if (inp.type !== 'datetime-local') {
                                    $(inp).datepicker({
                                            dateFormat: scope.format.toLowerCase().split(' ')[0]
                                        })
                                        .datepicker("setDate", scope.value)
                                        .on("dateChange", function(e) {
                                            scope.$apply(function() {
                                                scope.value = e.date;

                                                if (angular.isDefined(scope.$parent.Model)) {
                                                    scope.$parent.Model.$hasChanges = true;
                                                }
                                            });
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
        .directive('tbDateEditor', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<div ng-class="{ \'form-group\' : showLabel && isEditing, \'has-error\' : !$valid && $dirty() }">' +
                        '<span ng-hide="isEditing">{{ value | date: format }}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<input type="date" ng-show="isEditing" ng-model="value" class="form-control" ' +
                        'ng-required="required" ng-readonly="readOnly" name="{{name}}"/>' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">' +
                        '{{error}}' +
                        '</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: tubularEditorService.defaultScope,
                    controller: tubularEditorService.dateEditorController('yyyy-MM-dd'),
                    compile: function compile() {
                        return {
                            post: function(scope, lElement) {
                                var inp = $(lElement).find("input[type=date]")[0];
                                if (inp.type !== 'date') {
                                    $(inp).datepicker({
                                            dateFormat: scope.format.toLowerCase()
                                        })
                                        .datepicker("setDate", scope.value)
                                        .on("dateChange", function(e) {
                                            scope.$apply(function() {
                                                scope.value = e.date;

                                                if (angular.isDefined(scope.$parent.Model)) {
                                                    scope.$parent.Model.$hasChanges = true;
                                                }
                                            });
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
        .directive('tbDropdownEditor', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<div ng-class="{ \'form-group\' : showLabel && isEditing, \'has-error\' : !$valid && $dirty() }">' +
                        '<span ng-hide="isEditing">{{ value }}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<select ng-options="{{ selectOptions }}" ng-show="isEditing" ng-model="value" class="form-control" ' +
                        'ng-required="required" ng-disabled="readOnly" name="{{name}}" />' +
                        '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">' +
                        '{{error}}' +
                        '</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: angular.extend({ options: '=?', optionsUrl: '@', optionsMethod: '@?', optionLabel: '@?', optionKey: '@?' }, tubularEditorService.defaultScope),
                    controller: [
                        '$scope', function($scope) {
                            tubularEditorService.setupScope($scope);
                            $scope.dataIsLoaded = false;
                            $scope.selectOptions = "d for d in options";

                            if (angular.isDefined($scope.optionLabel)) {
                                $scope.selectOptions = "d." + $scope.optionLabel + " for d in options";

                                if (angular.isDefined($scope.optionKey)) {
                                    $scope.selectOptions = 'd.' + $scope.optionKey + ' as ' + $scope.selectOptions;
                                }
                            }

                            $scope.$watch('value', function(val) {
                                $scope.$emit('tbForm_OnFieldChange', $scope.$component, $scope.name, val);
                            });

                            $scope.loadData = function() {
                                if ($scope.dataIsLoaded) {
                                    return;
                                }

                                if (angular.isUndefined($scope.$component) || $scope.$component == null) {
                                    throw 'You need to define a parent Form or Grid';
                                }

                                var currentRequest = $scope.$component.dataService.retrieveDataAsync({
                                    serverUrl: $scope.optionsUrl,
                                    requestMethod: $scope.optionsMethod || 'GET'
                                });

                                var value = $scope.value;
                                $scope.value = '';

                                currentRequest.promise.then(
                                    function(data) {
                                        $scope.options = data;
                                        $scope.dataIsLoaded = true;
                                        // TODO: Add an attribute to define if autoselect is OK
                                        var possibleValue = $scope.options && $scope.options.length > 0 ?
                                            angular.isDefined($scope.optionKey) ? $scope.options[0][$scope.optionKey] : $scope.options[0]
                                            : '';
                                        $scope.value = value || $scope.defaultValue || possibleValue;
                                    }, function(error) {
                                        $scope.$emit('tbGrid_OnConnectionError', error);
                                    });
                            };

                            if (angular.isDefined($scope.optionsUrl)) {
                                $scope.$watch('optionsUrl', function() {
                                    $scope.dataIsLoaded = false;
                                    $scope.loadData();
                                });

                                if ($scope.isEditing) {
                                    $scope.loadData();
                                } else {
                                    $scope.$watch('isEditing', function() {
                                        if ($scope.isEditing) {
                                            $scope.loadData();
                                        }
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
            'tubularEditorService', '$q', function(tubularEditorService, $q) {

                return {
                    template: '<div ng-class="{ \'form-group\' : showLabel && isEditing, \'has-error\' : !$valid && $dirty() }">' +
                        '<span ng-hide="isEditing">{{ value }}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<div class="input-group" ng-show="isEditing">' +
                        '<input ng-model="value" placeholder="{{placeholder}}" title="{{tooltip}}" ' +
                        'class="form-control {{css}}" ng-readonly="readOnly || lastSet.indexOf(value) !== -1" typeahead="{{ selectOptions }}" ' +
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
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: angular.extend({
                        options: '=?',
                        optionsUrl: '@',
                        optionsMethod: '@?',
                        optionLabel: '@?',
                        css: '@?'
                    }, tubularEditorService.defaultScope),
                    controller: [
                        '$scope', function($scope) {
                            tubularEditorService.setupScope($scope);
                            $scope.selectOptions = "d for d in getValues($viewValue)";
                            $scope.lastSet = [];

                            if (angular.isDefined($scope.optionLabel)) {
                                $scope.selectOptions = "d as d." + $scope.optionLabel + " for d in getValues($viewValue)";
                            }

                            $scope.$watch('value', function(val) {
                                $scope.$emit('tbForm_OnFieldChange', $scope.$component, $scope.name, val);
                                $scope.tooltip = val;
                                if (angular.isDefined(val) && val != null && angular.isDefined($scope.optionLabel)) {
                                    $scope.tooltip = val[$scope.optionLabel];
                                }
                            });

                            $scope.getValues = function(val) {
                                if (angular.isDefined($scope.optionsUrl)) {
                                    if (angular.isUndefined($scope.$component) || $scope.$component == null) {
                                        throw 'You need to define a parent Form or Grid';
                                    }

                                    var p = $scope.$component.dataService.retrieveDataAsync({
                                        serverUrl: $scope.optionsUrl + '?search=' + val,
                                        requestMethod: $scope.optionsMethod || 'GET'
                                    }).promise;

                                    p.then(function(data) {
                                        $scope.lastSet = data;
                                        return data;
                                    });

                                    return p;
                                }

                                return $q(function(resolve) {
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
         * @param {string} name Set the field name.
         * @param {object} value Set the value.
         */
        .directive('tbHiddenField', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<input type="hidden" ng-model="value" class="form-control" name="{{name}}"  />',
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
         * @param {string} name Set the field name.
         * @param {object} value Set the value.
         * @param {object} checkedValue Set the checked value.
         * @param {object} uncheckedValue Set the unchecked value.
         * @param {boolean} isEditing Indicate if the field is showing editor.
         * @param {boolean} showLabel Set if the label should be display.
         * @param {string} label Set the field's label otherwise the name is used.
         * @param {string} help Set the help text.
         */
        .directive('tbCheckboxField', [
            'tubularEditorService', function(tubularEditorService) {

                return {
                    template: '<div ng-class="{ \'checkbox\' : isEditing, \'has-error\' : !$valid && $dirty() }" class="tubular-checkbox">' +
                        '<span ng-hide="isEditing">{{value ? checkedValue : uncheckedValue}}</span>' +
                        '<input ng-show="isEditing" type="checkbox" ng-model="value" ng-disabled="readOnly"' +
                        'class="tubular-checkbox" id="{{name}}" name="{{name}}" /> ' +
                        '<label ng-show="isEditing" for="{{name}}">' +
                        '{{label}}' +
                        '</label>' +
                        '<span class="help-block error-block" ng-show="isEditing" ' +
                        'ng-repeat="error in state.$errors">' +
                        '{{error}}' +
                        '</span>' +
                        '<span class="help-block" ng-show="isEditing && help">{{help}}</span>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: angular.extend({
                        checkedValue: '=?',
                        uncheckedValue: '=?'
                    }, tubularEditorService.defaultScope),
                    controller: [
                        '$scope', '$element', function ($scope) {
                            $scope.required = false; // overwrite required to false always
                            $scope.checkedValue = angular.isDefined($scope.checkedValue) ? $scope.checkedValue : true;
                            $scope.uncheckedValue = angular.isDefined($scope.uncheckedValue) ? $scope.uncheckedValue : false;

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
        .directive('tbTextArea', [
            'tubularEditorService', '$filter', function (tubularEditorService, $filter) {

                return {
                    template: '<div ng-class="{ \'form-group\' : showLabel && isEditing, \'has-error\' : !$valid && $dirty() }">' +
                        '<span ng-hide="isEditing">{{value}}</span>' +
                        '<label ng-show="showLabel">{{ label }}</label>' +
                        '<textarea ng-show="isEditing" placeholder="{{placeholder}}" ng-model="value" class="form-control" ' +
                        ' ng-required="required" ng-readonly="readOnly" name="{{name}}"></textarea>' +
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
                                if (angular.isDefined($scope.min) && angular.isDefined($scope.value) && $scope.value != null) {
                                    if ($scope.value.length < parseInt($scope.min)) {
                                        $scope.$valid = false;
                                        $scope.state.$errors = [$filter('translate')('EDITOR_MIN_CHARS', +$scope.min)];
                                        return;
                                    }
                                }

                                if (angular.isDefined($scope.max) && angular.isDefined($scope.value) && $scope.value != null) {
                                    if ($scope.value.length > parseInt($scope.max)) {
                                        $scope.$valid = false;
                                        $scope.state.$errors = [$filter('translate')('EDITOR_MAX_CHARS', +$scope.max)];
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
                template: '<div class="text-right">' +
                        '<a class="btn btn-sm btn-success" ng-click="applyFilter()" ng-disabled="filter.Operator == \'None\'">{{\'CAPTION_APPLY\' | translate}}</a>&nbsp;' +
                        '<button class="btn btn-sm btn-danger" ng-click="clearFilter()">{{\'CAPTION_CLEAR\' | translate}}</button>' +
                        '</div>',
                restrict: 'E',
                replace: true,
                transclude: true
            };
        }])
        /**
         * @ngdoc directive
         * @name tbColumnSelector
         * @restrict E
         *
         * @description
         * The `tbColumnSelector` is a button to show columns selector popup.
         */
        .directive('tbColumnSelector', [function () {
            return {
                template: '<button class="btn btn-sm btn-default" ng-click="openColumnsSelector()">{{\'CAPTION_SELECTCOLUMNS\' | translate}}</button></div>',
                restrict: 'E',
                replace: true,
                transclude: true,
                controller: ['$scope', '$uibModal', function ($scope, $modal) {
                    $scope.$component = $scope.$parent;

                    $scope.openColumnsSelector = function () {
                        var model = $scope.$component.columns;

                        var dialog = $modal.open({
                            template: '<div class="modal-header">' +
                                '<h3 class="modal-title">{{\'CAPTION_SELECTCOLUMNS\' | translate}}</h3>' +
                                '</div>' +
                                '<div class="modal-body">' +
                                '<table class="table table-bordered table-responsive table-striped table-hover table-condensed">' +
                                '<thead><tr><th>Visible?</th><th>Name</th><th>Grouping?</th></tr></thead>' +
                                '<tbody><tr ng-repeat="col in Model">' +
                                '<td><input type="checkbox" ng-model="col.Visible" ng-disabled="col.Visible && isInvalid()" /></td>' +
                                '<td>{{col.Label}}</td>' +
                                '<td><input type="checkbox" ng-disabled="true" ng-model="col.IsGrouping" /></td>' +
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
                }]
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
         * 
         * @param {string} text Set the search text.
         * @param {string} operator Set the initial operator, default depends on data type.
         */
        .directive('tbColumnFilter', [
            'tubularGridFilterService', function(tubularGridFilterService) {

                return {
                    require: '^tbColumn',
                    template: '<div class="tubular-column-menu">' +
                        '<button class="btn btn-xs btn-default btn-popover" ng-click="open()" ' +
                        'ng-class="{ \'btn-success\': filter.HasFilter }">' +
                        '<i class="fa fa-filter"></i></button>' +
                        '<div style="display: none;">' +
                        '<button type="button" class="close" data-dismiss="modal" ng-click="close()"><span aria-hidden="true">×</span></button>' +
                        '<h4>{{filterTitle}}</h4>' +
                        '<form class="tubular-column-filter-form" onsubmit="return false;">' +
                        '<select class="form-control" ng-model="filter.Operator" ng-hide="dataType == \'boolean\'"></select>&nbsp;' +
                        '<input class="form-control" type="search" ng-model="filter.Text" autofocus ng-keypress="checkEvent($event)" ng-hide="dataType == \'boolean\'"' +
                        'placeholder="{{\'CAPTION_VALUE\' | translate}}" ng-disabled="filter.Operator == \'None\'" />' +
                        '<div class="text-center" ng-show="dataType == \'boolean\'">' +
                        '<button type="button" class="btn btn-default btn-md" ng-disabled="filter.Text === true" ng-click="filter.Text = true; filter.Operator = \'Equals\';">' +
                        '<i class="fa fa-check"></i></button>&nbsp;' +
                        '<button type="button" class="btn btn-default btn-md" ng-disabled="filter.Text === false" ng-click="filter.Text = false; filter.Operator = \'Equals\';">' +
                        '<i class="fa fa-times"></i></button></div>' +
                        '<input type="search" class="form-control" ng-model="filter.Argument[0]" ng-keypress="checkEvent($event)" ng-show="filter.Operator == \'Between\'" />' +
                        '<hr />' +
                        '<tb-column-filter-buttons></tb-column-filter-buttons>' +
                        '</form></div>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false,
                    compile: function compile() {
                        return {
                            pre: function(scope, lElement, lAttrs) {
                                tubularGridFilterService.applyFilterFuncs(scope, lElement, lAttrs);
                            },
                            post: function(scope, lElement, lAttrs) {
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
         * @param {string} text Set the search text.
         * @param {object} argument Set the search object (if the search is text use text attribute).
         * @param {string} operator Set the initial operator, default depends on data type.
         */
        .directive('tbColumnDateTimeFilter', [
            'tubularGridFilterService', function(tubularGridFilterService) {

                return {
                    require: '^tbColumn',
                    template: '<div ngTransclude class="btn-group tubular-column-menu">' +
                        '<button class="btn btn-xs btn-default btn-popover" ng-click="open()" ' +
                        'ng-class="{ \'btn-success\': filter.HasFilter }">' +
                        '<i class="fa fa-filter"></i></button>' +
                        '<div style="display: none;">' +
                        '<button type="button" class="close" data-dismiss="modal" ng-click="close()"><span aria-hidden="true">×</span></button>' +
                        '<h4>{{filterTitle}}</h4>' +
                        '<form class="tubular-column-filter-form" onsubmit="return false;">' +
                        '<select class="form-control" ng-model="filter.Operator"></select>' +
                        '<input type="date" class="form-control" ng-model="filter.Text" ng-keypress="checkEvent($event)" />&nbsp;' +
                        '<input type="date" class="form-control" ng-model="filter.Argument[0]" ng-keypress="checkEvent($event)" ' +
                        'ng-show="filter.Operator == \'Between\'" />' +
                        '<hr />' +
                        '<tb-column-filter-buttons></tb-column-filter-buttons>' +
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
                    compile: function compile() {
                        return {
                            pre: function(scope, lElement, lAttrs) {
                                tubularGridFilterService.applyFilterFuncs(scope, lElement, lAttrs, function() {
                                    var inp = $(lElement).find("input[type=date]")[0];

                                    if (inp.type !== 'date') {
                                        $(inp).datepicker({
                                            dateFormat: scope.format.toLowerCase()
                                        }).on("dateChange", function(e) {
                                            scope.filter.Text = e.date;
                                        });
                                    }

                                    var inpLev = $(lElement).find("input[type=date]")[1];

                                    if (inpLev.type !== 'date') {
                                        $(inpLev).datepicker({
                                            dateFormat: scope.format.toLowerCase()
                                        }).on("dateChange", function(e) {
                                            scope.filter.Argument = [e.date];
                                        });
                                    }
                                });
                            },
                            post: function(scope, lElement, lAttrs) {
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
         * @scope
         * 
         * @param {object} argument Set the search object.
         * @param {string} operator Set the initial operator, default depends on data type.
         * @param {string} optionsUrl Set the URL to retrieve options
         */
        .directive('tbColumnOptionsFilter', [
            'tubularGridFilterService', function(tubularGridFilterService) {

                return {
                    require: '^tbColumn',
                    template: '<div class="tubular-column-menu">' +
                        '<button class="btn btn-xs btn-default btn-popover" ng-click="open()" ' +
                        'ng-class="{ \'btn-success\': filter.HasFilter }">' +
                        '<i class="fa fa-filter"></i></button>' +
                        '<div style="display: none;">' +
                        '<button type="button" class="close" data-dismiss="modal" ng-click="close()"><span aria-hidden="true">×</span></button>' +
                        '<h4>{{::filterTitle}}</h4>' +
                        '<form class="tubular-column-filter-form" onsubmit="return false;">' +
                        '<select class="form-control checkbox-list" ng-model="filter.Argument" ng-options="item for item in optionsItems" ' +
                        ' multiple ng-disabled="dataIsLoaded == false"></select>' +
                        '<hr />' + 
                        '<tb-column-filter-buttons></tb-column-filter-buttons>' +
                        '</form></div>' +
                        '</div>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: false,
                    controller: [
                        '$scope', function($scope) {
                            $scope.dataIsLoaded = false;
                            
                            $scope.getOptionsFromUrl = function () {
                                if ($scope.dataIsLoaded) {
                                    $scope.$apply();
                                    return;
                                }

                                var currentRequest = $scope.$component.dataService.retrieveDataAsync({
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
                    compile: function compile() {
                        return {
                            pre: function(scope, lElement, lAttrs) {
                                tubularGridFilterService.applyFilterFuncs(scope, lElement, lAttrs, function() {
                                    scope.getOptionsFromUrl();
                                });
                            },
                            post: function(scope, lElement, lAttrs) {
                                tubularGridFilterService.createFilterModel(scope, lAttrs);

                                scope.filter.Operator = 'Multiple';
                            }
                        };
                    }
                };
            }
        ]);
})();
(function () {
    'use strict';

    angular.module('tubular.directives')
        /**
         * @ngdoc directive
         * @name tbForm
         * @restrict E
         *
         * @description
         * The `tbForm` directive is the base to create any form. You can define a `dataService` and a
         * `modelKey` to auto-load a record. The `serverSaveUrl` can be used to create a new or update
         * an existing record.
         * 
         * @scope
         * 
         * @param {string} serverUrl Set the HTTP URL where the data comes.
         * @param {string} serverSaveUrl Set the HTTP URL where the data will be saved.
         * @param {string} serverSaveMethod Set HTTP Method to save data.
         * @param {object} model The object model to show in the form.
         * @param {boolean} isNew Set if the form is for create a new record.
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
                        isNew: '@',
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

                                $scope.model = new TubularModel($scope, data, $scope.dataService);
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
                                                $scope.model = new TubularModel($scope, data, $scope.dataService);
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

                                                $scope.model = new TubularModel(innerScope, data, dataService);
                                                $scope.bindFields();
                                                $scope.model.$isNew = true;
                                            }, function (error) {
                                                $scope.$emit('tbForm_OnConnectionError', error);
                                            });
                                    }

                                    return;
                                }

                                if (angular.isUndefined($scope.model)) {
                                    $scope.model = new TubularModel($scope, {}, $scope.dataService);
                                }

                                $scope.bindFields();
                            };

                            $scope.save = function () {
                                if (!$scope.model.$valid()) {
                                    return;
                                }

                                $scope.currentRequest = $scope.model.save();

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
                                        }, function (error) {
                                            $scope.$emit('tbForm_OnConnectionError', error, $scope);
                                        })
                                    .then(function () {
                                        $scope.model.$isLoading = false;
                                        $scope.currentRequest = null;
                                    });
                            };

                            $scope.update = function () {
                                $scope.save();
                            };

                            $scope.create = function () {
                                $scope.model.$isNew = true;
                                $scope.save();
                            };

                            $scope.cancel = function () {
                                $scope.$emit('tbForm_OnCancel', $scope.model);
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
})();
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
         * 
         * @param {number} minChars How many chars before to search, default 3.
         */
        .directive('tbTextSearch', [function() {
            return {
                require: '^tbGrid',
                template:
                    '<div class="tubular-grid-search">' +
                        '<div class="input-group input-group-sm">' +
                        '<span class="input-group-addon"><i class="glyphicon glyphicon-search"></i></span>' +
                        '<input type="search" class="form-control" placeholder="{{:: placeholder || (\'UI_SEARCH\' | translate) }}" maxlength="20" ' +
                        'ng-model="$component.search.Text" ng-model-options="{ debounce: 300 }">' +
                        '<span class="input-group-btn" ng-show="$component.search.Text.length > 0">' +
                        '<button class="btn btn-default" tooltip="{{\'CAPTION_CLEAR\' | translate}}" ng-click="$component.search.Text = \'\'">' +
                        '<i class="fa fa-times-circle"></i>' +
                        '</button>' +
                        '</span>' +
                        '<div>' +
                        '<div>',
                restrict: 'E',
                replace: true,
                transclude: false,
                scope: {
                    minChars: '@?',
                    placeholder: '@'
                },
                terminal: false,
                controller: [
                    '$scope', function($scope) {
                        $scope.$component = $scope.$parent.$parent;
                        $scope.minChars = $scope.minChars || 3;
                        $scope.tubularDirective = 'tubular-grid-text-search';
                        $scope.lastSearch = $scope.$component.search.Text;

                        $scope.$watch("$component.search.Text", function(val, prev) {
                            if (angular.isUndefined(val) || val === prev) {
                                return;
                            }
                            
                            if ($scope.lastSearch !== "" && val === "") {
                                $scope.$component.saveSearch();
                                $scope.$component.search.Operator = 'None';
                                $scope.$component.retrieveData();
                                return;
                            }

                            if (val === "" || val.length < $scope.minChars) {
                                return;
                            }

                            if (val === $scope.lastSearch) {
                                return;
                            }

                            $scope.lastSearch = val;
                            $scope.$component.saveSearch();
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
         * @param {string} cancelCaption Set the caption to use in the Cancel button, default Cancel.
         * @param {string} legend Set the legend to warn user, default 'Do you want to delete this row?'.
         * @param {string} icon Set the CSS icon's class, the button can have only icon.
         */
        .directive('tbRemoveButton', ['$compile', function($compile) {

            return {
                require: '^tbGrid',
                template: '<button ng-click="confirmDelete()" class="btn" ng-hide="model.$isEditing">' +
                    '<span ng-show="showIcon" class="{{::icon}}"></span>' +
                    '<span ng-show="showCaption">{{:: caption || (\'CAPTION_REMOVE\' | translate) }}</span>' +
                    '</button>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    model: '=',
                    caption: '@',
                    cancelCaption: '@',
                    legend: '@',
                    icon: '@'
                },
                controller: [
                    '$scope', '$element', '$filter', function($scope, $element, $filter) {
                        $scope.showIcon = angular.isDefined($scope.icon);
                        $scope.showCaption = !($scope.showIcon && angular.isUndefined($scope.caption));
                        $scope.confirmDelete = function() {
                            $element.popover({
                                html: true,
                                title: $scope.legend || $filter('translate')('UI_REMOVEROW'),
                                content: function() {
                                    var html = '<div class="tubular-remove-popover">' +
                                        '<button ng-click="model.delete()" class="btn btn-danger btn-xs">' + ($scope.caption || $filter('translate')('CAPTION_REMOVE')) + '</button>' +
                                        '&nbsp;<button ng-click="cancelDelete()" class="btn btn-default btn-xs">' + ($scope.cancelCaption || $filter('translate')('CAPTION_CANCEL')) + '</button>' +
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
         */
        .directive('tbEditButton', [function() {

            return {
                require: '^tbGrid',
                template: '<button ng-click="edit()" class="btn btn-default" ' +
                    'ng-hide="model.$isEditing">{{:: caption || (\'CAPTION_EDIT\' | translate) }}</button>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    model: '=',
                    caption: '@'
                },
                controller: [
                    '$scope', function($scope) {
                        $scope.component = $scope.$parent.$parent.$component;

                        $scope.edit = function() {
                            if ($scope.component.editorMode === 'popup') {
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
         * @param {array} options Set the page options array, default [10, 20, 50, 100].
         */
        .directive('tbPageSizeSelector', [function() {

            return {
                require: '^tbGrid',
                template: '<div class="{{::css}}"><form class="form-inline">' +
                    '<div class="form-group">' +
                    '<label class="small">{{:: caption || (\'UI_PAGESIZE\' | translate) }} </label>&nbsp;' +
                    '<select ng-model="$parent.$parent.pageSize" class="form-control input-sm {{::selectorCss}}" ' +
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
                        $scope.options = angular.isDefined($scope.options) ? $scope.options : [10, 20, 50, 100];
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
         * @param {string} caption Set the caption.
         * @param {string} captionMenuCurrent Set the caption.
         * @param {string} captionMenuAll Set the caption.
         */
        .directive('tbExportButton', [function() {

            return {
                require: '^tbGrid',
                template: '<div class="btn-group">' +
                    '<button class="btn btn-default dropdown-toggle {{::css}}" data-toggle="dropdown" aria-expanded="false">' +
                    '<span class="fa fa-download"></span>&nbsp;{{:: caption || (\'UI_EXPORTCSV\' | translate)}}&nbsp;<span class="caret"></span>' +
                    '</button>' +
                    '<ul class="dropdown-menu" role="menu">' +
                    '<li><a href="javascript:void(0)" ng-click="downloadCsv($parent)">{{:: captionMenuCurrent || (\'UI_CURRENTROWS\' | translate)}}</a></li>' +
                    '<li><a href="javascript:void(0)" ng-click="downloadAllCsv($parent)">{{:: captionMenuAll || (\'UI_ALLROWS\' | translate)}}</a></li>' +
                    '</ul>' +
                    '</div>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    filename: '@',
                    css: '@',
                    caption: '@',
                    captionMenuCurrent: '@',
                    captionMenuAll: '@'
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
         * @param {string} caption Set the caption.
         */
        .directive('tbPrintButton', [function() {

            return {
                require: '^tbGrid',
                template: '<button class="btn btn-default" ng-click="printGrid()">' +
                    '<span class="fa fa-print"></span>&nbsp;{{caption || (\'CAPTION_PRINT\' | translate)}}' +
                    '</button>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    title: '@',
                    printCss: '@',
                    caption: '@'
                },
                controller: [
                    '$scope', function($scope) {
                        $scope.$component = $scope.$parent.$parent;
                        
                        $scope.printGrid = function() {
                            $scope.$component.getFullDataSource(function(data) {
                                var tableHtml = "<table class='table table-bordered table-striped'><thead><tr>"
                                    + $scope.$component.columns
                                    .filter(function (c) { return c.Visible; })
                                    .map(function (el) {
                                        return "<th>" + (el.Label || el.Name) + "</th>";
                                    }).join(" ")
                                    + "</tr></thead>"
                                    + "<tbody>"
                                    + data.map(function (row) {
                                        if (typeof (row) === 'object') {
                                            row = $.map(row, function(el) { return el; });
                                        }

                                        return "<tr>" + row.map(function(cell, index) {
                                            if (angular.isDefined($scope.$component.columns[index]) &&
                                            !$scope.$component.columns[index].Visible) {
                                                return "";
                                            }

                                            return "<td>" + cell + "</td>";
                                        }).join(" ") + "</tr>";
                                    }).join(" ")
                                    + "</tbody>"
                                    + "</table>";

                                var popup = window.open("about:blank", "Print", "menubar=0,location=0,height=500,width=800");
                                popup.document.write('<link rel="stylesheet" href="//cdn.jsdelivr.net/bootstrap/latest/css/bootstrap.min.css" />');

                                if ($scope.printCss != '') {
                                    popup.document.write('<link rel="stylesheet" href="' + $scope.printCss + '" />');
                                }

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
                            '<uib-pagination ng-disabled="$component.isEmpty" direction-links="true" ' +
                            'boundary-links="true" total-items="$component.filteredRecordCount" ' +
                            'items-per-page="$component.pageSize" max-size="5" ng-model="$component.currentPage" ng-change="pagerPageChanged()">' +
                            '</uib-pagination>' +
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

                            $scope.$watch('$component.currentPage', function () {
                                if ($scope.$component.currentPage != $scope.$component.requestedPage) {
                                    $scope.$component.requestedPage = $scope.$component.currentPage;
                                }
                            });

                            $scope.pagerPageChanged = function () {
                                $scope.$component.requestedPage = $scope.$component.currentPage;
                                var allLinks = $element.find('li a');
                                $(allLinks).blur();
                            };
                        }
                    ],
                    compile: function compile() {
                        return {
                            post: function (scope, lElement, lAttrs) {
                                scope.firstButtonClass = lAttrs.firstButtonClass || 'fa fa-fast-backward';
                                scope.prevButtonClass = lAttrs.prevButtonClass || 'fa fa-backward';

                                scope.nextButtonClass = lAttrs.nextButtonClass || 'fa fa-forward';
                                scope.lastButtonClass = lAttrs.lastButtonClass || 'fa fa-fast-forward';

                                var timer = $timeout(function () {
                                    var allLinks = lElement.find('li a');

                                    $(allLinks[0]).html('<i class="' + scope.firstButtonClass + '"></i>');
                                    $(allLinks[1]).html('<i class="' + scope.prevButtonClass + '"></i>');

                                    $(allLinks[allLinks.length - 2]).html('<i class="' + scope.nextButtonClass + '"></i>');
                                    $(allLinks[allLinks.length - 1]).html('<i class="' + scope.lastButtonClass + '"></i>');
                                }, 0);

                                scope.$on('$destroy', function () { $timeout.cancel(timer); });
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
                    template: '<div class="pager-info small" ng-hide="$component.isEmpty">' +
                        '{{\'UI_SHOWINGRECORDS\' | translate: currentInitial:currentTop:$component.filteredRecordCount}} ' +
                        '<span ng-show="filtered">' +
                        '{{\'UI_FILTEREDRECORDS\' | translate: $component.totalRecordCount}}</span>' +
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

                                if ($scope.currentInitial < 0 || $scope.$component.totalRecordCount === 0) {
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
(function() {
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
        * @name tubularGridColumnModel
        *
        * @description
        * The `tubularGridColumnModel` factory is the base to generate a column model to use with `tbGrid`.
        * 
        * This model doesn't need to be created in your controller, the `tbGrid` generate it from any `tbColumn`.
        */
        .factory('tubularGridColumnModel', ["$filter", function($filter) {

            var parseSortDirection = function(value) {
                if (angular.isUndefined(value)) {
                    return 'None';
                }

                if (value.toLowerCase().indexOf('asc') === 0) {
                    return 'Ascending';
                }

                if (value.toLowerCase().indexOf('desc') === 0) {
                    return 'Descending';
                }

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
                this.Aggregate = attrs.aggregate || "none";
                this.MetaAggregate = attrs.metaAggregate || "none";

                this.FilterOperators = {
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
            };
        }])
        /**
        * @ngdoc factory
        * @name tubularGridFilterModel
        *
        * @description
        * The `tubularGridFilterModel` factory is the base to generate a filter model to use with `tbGrid`.
        * 
        * This model doesn't need to be created in your controller, the `tubularGridFilterService` generate it.
        */
        .factory('tubularGridFilterModel', function() {

            return function(attrs) {
                this.Text = attrs.text || null;
                this.Argument = null;

                if (attrs.argument) {
                    this.Argument = [attrs.argument];
                }

                this.Operator = attrs.operator || 'Contains';
                this.OptionsUrl = attrs.optionsUrl || null;
                this.HasFilter = !(this.Text == null);
            };
        })
        /**
        * @ngdoc factory
        * @name tubularModel
        *
        * @description
        * The `tubularModel` factory is the base to generate a row model to use with `tbGrid` and `tbForm`.
        */
        .factory('tubularModel', function() {
            return function($scope, data, dataService) {
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

                if (angular.isDefined($scope.columns)) {
                    angular.forEach($scope.columns, function(col, key) {
                        var value = data[key] || data[col.Name];

                        if (angular.isUndefined(value) && data[key] === 0) {
                            value = 0;
                        }

                        obj.$addField(col.Name, value);

                        if (col.DataType === "date" || col.DataType === "datetime" || col.DataType === "datetimeutc") {
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
                    for (var k in obj.$state) {
                        if (obj.$state.hasOwnProperty(k)) {
                            var key = k;
                            if (angular.isUndefined(obj.$state[key]) ||
                                obj.$state[key] == null ||
                                angular.isUndefined(obj.$state[key].$valid)) {
                                continue;
                            }

                            if (obj.$state[key].$valid() && obj.$state[key].$dirty) {
                                continue;
                            }

                            return false;
                        }
                    }

                    return true;
                };

                // Returns a save promise
                obj.save = function() {
                    if (angular.isUndefined(dataService) || dataService == null) {
                        throw 'Define DataService to your model.';
                    }

                    if (angular.isUndefined($scope.serverSaveUrl) || $scope.serverSaveUrl == null) {
                        throw 'Define a Save URL.';
                    }

                    if (!obj.$isNew && !obj.$hasChanges) {
                        return false;
                    }

                    obj.$isLoading = true;

                    return dataService.saveDataAsync(obj, {
                        serverUrl: $scope.serverSaveUrl,
                        requestMethod: obj.$isNew ? ($scope.serverSaveMethod || 'POST') : 'PUT'
                    }).promise;
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
})();
(function () {
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

                                $scope.savePopup = function (innerModel) {
                                    innerModel = innerModel || $scope.Model;

                                    // If we have nothing to save and it's not a new record, just close
                                    if (!innerModel.$isNew && !innerModel.$hasChanges) {
                                        $scope.closePopup();
                                        return null;
                                    }

                                    var result = innerModel.save();

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
         * Use `tubularGridExportService` to export your `tbGrid` to CSV format.
         */
        .service('tubularGridExportService', function tubularGridExportService() {
            var me = this;

            me.getColumns = function (gridScope) {
                return gridScope.columns
                    .map(function (c) { return c.Name.replace(/([a-z])([A-Z])/g, '$1 $2'); });
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
         * @name tubularGridFilterService
         *
         * @description
         * The `tubularGridFilterService` service is a internal helper to setup any `FilterModel` with a UI.
         */
        .service('tubularGridFilterService', [
            'tubularGridFilterModel', '$compile', '$filter', function tubularGridFilterService(FilterModel, $compile, $filter) {
                var me = this;

                me.applyFilterFuncs = function (scope, el, attributes, openCallback) {
                    scope.$component = scope.$parent.$component;

                    scope.$watch('filter.Operator', function (val) {
                        if (val === 'None') scope.filter.Text = '';
                    });

                    scope.$watch(function () {
                        var columns = scope.$component.columns.filter(function (el) {
                            return el.Name === scope.filter.Name;
                        });

                        return columns.length !== 0 ? columns[0] : null;
                    }, function (val) {
                        if (val && val != null) {
                            if (scope.filter.HasFilter != val.Filter.HasFilter) {
                                scope.filter.HasFilter = val.Filter.HasFilter;
                                scope.filter.Text = val.Filter.Text;
                                scope.retrieveData();
                            }
                        }
                    }, true);

                    scope.retrieveData = function () {
                        var columns = scope.$component.columns.filter(function (el) {
                            return el.Name === scope.filter.Name;
                        });

                        if (columns.length !== 0) {
                            columns[0].Filter = scope.filter;
                        }

                        scope.$component.retrieveData();
                        scope.close();
                    };

                    scope.clearFilter = function () {
                        if (scope.filter.Operator != 'Multiple') {
                            scope.filter.Operator = 'None';
                        }

                        scope.filter.Text = '';
                        scope.filter.Argument = [];
                        scope.filter.HasFilter = false;
                        scope.retrieveData();
                    };

                    scope.applyFilter = function () {
                        scope.filter.HasFilter = true;
                        scope.retrieveData();
                    };

                    scope.close = function () {
                        $(el).find('.btn-popover').popover('hide');
                    };

                    scope.open = function () {
                        $(el).find('.btn-popover').popover('toggle');
                    };

                    scope.checkEvent = function (keyEvent) {
                        if (keyEvent.which === 13) {
                            scope.applyFilter();
                            keyEvent.preventDefault();
                        }
                    };

                    $(el).find('.btn-popover').popover({
                        html: true,
                        placement: 'bottom',
                        trigger: 'manual',
                        content: function () {
                            var selectEl = $(this).next().find('select').find('option').remove().end();
                            angular.forEach(scope.filterOperators, function (val, key) {
                                $(selectEl).append('<option value="' + key + '">' + val + '</option>');
                            });

                            return $compile($(this).next().html())(scope);
                        }
                    });

                    $(el).find('.btn-popover').on('show.bs.popover', function (e) {
                        $('.btn-popover').not(e.target).popover("hide");
                    });

                    if (angular.isDefined(openCallback)) {
                        $(el).find('.btn-popover').on('shown.bs.popover', openCallback);
                    }
                };

                /**
                 * Creates a `FilterModel` using a scope and an Attributes array
                 */
                me.createFilterModel = function (scope, lAttrs) {
                    scope.filter = new FilterModel(lAttrs);
                    scope.filter.Name = scope.$parent.column.Name;
                    var columns = scope.$component.columns.filter(function (el) {
                        return el.Name === scope.filter.Name;
                    });

                    if (columns.length === 0) return;

                    scope.$watch('filter', function (n) {
                        if (columns[0].Filter.Text != n.Text) {
                            n.Text = columns[0].Filter.Text;

                            if (columns[0].Filter.Operator != n.Operator) {
                                n.Operator = columns[0].Filter.Operator;
                            }
                        }

                        scope.filter.HasFilter = columns[0].Filter.HasFilter;
                    });

                    columns[0].Filter = scope.filter;
                    scope.dataType = columns[0].DataType;
                    scope.filterOperators = columns[0].FilterOperators[scope.dataType];

                    if (scope.dataType === 'date' || scope.dataType === 'datetime' || scope.dataType === 'datetimeutc') {
                        scope.filter.Argument = [new Date()];

                        if (scope.filter.Operator === 'Contains') {
                            scope.filter.Operator = 'Equals';
                        }
                    }

                    if (scope.dataType === 'numeric' || scope.dataType === 'boolean') {
                        scope.filter.Argument = [1];

                        if (scope.filter.Operator === 'Contains') {
                            scope.filter.Operator = 'Equals';
                        }
                    }

                    scope.filterTitle = lAttrs.title || $filter('translate')('CAPTION_FILTER');
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
            '$filter', function tubularEditorService($filter) {
                var me = this;

                /*
                 * Returns the Default Scope parameters
                 */
                me.defaultScope = {
                    value: '=?',
                    isEditing: '=?',
                    editorType: '@',
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
                    defaultValue: '@?'
                };

                /**
                 * Setups a basic Date Editor Controller
                 * @param {string} format 
                 * @returns {array}  The controller definition
                 */
                me.dateEditorController = function (format) {
                    return [
                        '$scope', function (innerScope) {
                            innerScope.DataType = "date";

                            innerScope.$watch('value', function (val) {
                                if (typeof (val) === 'string') {
                                    innerScope.value = new Date(val);
                                }
                            });

                            innerScope.validate = function () {
                                if (angular.isDefined(innerScope.min)) {
                                    if (Object.prototype.toString.call(innerScope.min) !== "[object Date]") {
                                        innerScope.min = new Date(innerScope.min);
                                    }

                                    innerScope.$valid = innerScope.value >= innerScope.min;

                                    if (!innerScope.$valid) {
                                        innerScope.state.$errors = [$filter('translate')('EDITOR_MIN_DATE', $filter('date')(innerScope.min, innerScope.format))];
                                    }
                                }

                                if (!innerScope.$valid) {
                                    return;
                                }

                                if (angular.isDefined(innerScope.max)) {
                                    if (Object.prototype.toString.call(innerScope.max) !== "[object Date]") {
                                        innerScope.max = new Date(innerScope.max);
                                    }

                                    innerScope.$valid = innerScope.value <= innerScope.max;

                                    if (!innerScope.$valid) {
                                        innerScope.state.$errors = [$filter('translate')('EDITOR_MAX_DATE', $filter('date')(innerScope.max, innerScope.format))];
                                    }
                                }
                            };

                            me.setupScope(innerScope, format);
                        }
                    ];
                };

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
                me.setupScope = function (scope, defaultFormat) {
                    scope.isEditing = angular.isUndefined(scope.isEditing) ? true : scope.isEditing;
                    scope.showLabel = scope.showLabel || false;
                    scope.label = scope.label || (scope.name || '').replace(/([a-z])([A-Z])/g, '$1 $2');
                    scope.required = scope.required || false;
                    scope.readOnly = scope.readOnly || false;
                    scope.format = scope.format || defaultFormat;
                    scope.$valid = true;

                    // Get the field reference using the Angular way
                    scope.getFormField = function () {
                        var formScope = scope.$parent.$parent.getFormScope();

                        return formScope == null ? null : formScope[scope.Name];
                    };

                    scope.$dirty = function () {
                        // Just forward the property
                        var formField = scope.getFormField();

                        return formField == null ? true : formField.$dirty;
                    };

                    scope.checkValid = function () {
                        scope.$valid = true;
                        scope.state.$errors = [];

                        if ((angular.isUndefined(scope.value) && scope.required) ||
                        (Object.prototype.toString.call(scope.value) === "[object Date]" && isNaN(scope.value.getTime()) && scope.required)) {
                            scope.$valid = false;

                            // Although this property is invalid, if it is not $dirty
                            // then there should not be any errors for it
                            if (scope.$dirty()) {
                                scope.state.$errors = [$filter('translate')('EDITOR_REQUIRED')];
                            }

                            if (angular.isDefined(scope.$parent.Model)) {
                                scope.$parent.Model.$state[scope.Name] = scope.state;
                            }

                            return;
                        }

                        // Check if we have a validation function, otherwise return
                        if (angular.isUndefined(scope.validate)) {
                            return;
                        }

                        scope.validate();
                    };

                    // HACK: I need to know why
                    scope.$watch('label', function (n, o) {
                        if (angular.isUndefined(n)) {
                            scope.label = (scope.name || '').replace(/([a-z])([A-Z])/g, '$1 $2');
                        }
                    });

                    scope.$watch('value', function (newValue, oldValue) {
                        if (angular.isUndefined(oldValue) && angular.isUndefined(newValue)) {
                            return;
                        }

                        // This is the state API for every property in the Model
                        scope.state = {
                            $valid: function () {
                                scope.checkValid();
                                return this.$errors.length === 0;
                            },
                            $dirty: function () {
                                return scope.$dirty;
                            },
                            $errors: []
                        };

                        scope.$valid = true;

                        // Try to match the model to the parent, if it exists
                        if (angular.isDefined(scope.$parent.Model)) {
                            if (angular.isDefined(scope.$parent.Model[scope.name])) {
                                scope.$parent.Model[scope.name] = newValue;

                                if (angular.isUndefined(scope.$parent.Model.$state)) {
                                    scope.$parent.Model.$state = [];
                                }

                                scope.$parent.Model.$state[scope.Name] = scope.state;
                            } else if (angular.isDefined(scope.$parent.Model.$addField)) {
                                scope.$parent.Model.$addField(scope.name, newValue, true);
                            }
                        }

                        scope.checkValid();
                    });

                    var parent = scope.$parent;

                    // We try to find a Tubular Form in the parents
                    while (true) {
                        if (parent == null) break;
                        if (angular.isDefined(parent.tubularDirective) &&
                            (parent.tubularDirective === 'tubular-form' ||
                            parent.tubularDirective === 'tubular-rowset')) {

                            if (scope.name === null) {
                                return;
                            }

                            if (parent.hasFieldsDefinitions !== false) {
                                throw 'Cannot define more fields. Field definitions have been sealed';
                            }

                            scope.$component = parent.tubularDirective === 'tubular-form' ? parent : parent.$component;

                            scope.Name = scope.name;

                            scope.bindScope = function () {
                                scope.$parent.Model = parent.model;

                                if (angular.equals(scope.value, parent.model[scope.Name]) === false) {
                                    if (angular.isDefined(parent.model[scope.Name])) {
                                        scope.value = (scope.DataType === 'date' && parent.model[scope.Name] != null) ?
                                            new Date(parent.model[scope.Name]) :
                                            parent.model[scope.Name];
                                    }

                                    parent.$watch(function () {
                                        return scope.value;
                                    }, function (value) {
                                        parent.model[scope.Name] = value;
                                    });
                                }

                                if ((!scope.value || scope.value == null) && (scope.defaultValue && scope.defaultValue != null)) {
                                    if (scope.DataType === 'date' && scope.defaultValue != null) {
                                        scope.defaultValue = new Date(scope.defaultValue);
                                    }
                                    if (scope.DataType === 'numeric' && scope.defaultValue != null) {
                                        scope.defaultValue = parseFloat(scope.defaultValue);
                                    }

                                    scope.value = scope.defaultValue;
                                }

                                if (angular.isUndefined(parent.model.$state)) {
                                    parent.model.$state = {};
                                }

                                // This is the state API for every property in the Model
                                parent.model.$state[scope.Name] = {
                                    $valid: function () {
                                        scope.checkValid();
                                        return this.$errors.length === 0;
                                    },
                                    $dirty: function () {
                                        return scope.$dirty;
                                    },
                                    $errors: []
                                };

                                if (angular.equals(scope.state, parent.model.$state[scope.Name]) === false) {
                                    scope.state = parent.model.$state[scope.Name];
                                }
                            };

                            parent.fields.push(scope);

                            break;
                        }

                        parent = parent.$parent;
                    }
                };
            }
        ]);
})();
(function() {
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

                    request.timeout = request.timeout || 15000;

                    var timeoutHanlder = $timeout(function () {
                        cancel('Timed out');
                    }, request.timeout);

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
                            promise: $q(function (resolve, reject) {
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
                            promise: $q(function (resolve, reject) {
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
})();
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
                            promise: $q(function (resolve, reject) {
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
                        .map(function (el) { return el.Name + " " + (el.SortDirection == "Descending" ? "desc" : ""); });

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
        .service('tubularTemplateService', ['$templateCache',
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
(function() {
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
                        'EDITOR_MAX_DATE': "La fecha maxima es {0}.",
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
})();
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