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
        .constant("tubularConst", {
            "upCssClass": "fa-long-arrow-up",
            "downCssClass": "fa-long-arrow-down"
        })
        .filter('errormessage', function () {
            return function (input) {
                if (angular.isDefined(input) && angular.isDefined(input.data) &&
                    input.data != null &&
                    angular.isDefined(input.data.ExceptionMessage))
                    return input.data.ExceptionMessage;

                return input.statusText || "Connection Error";
            };
        }).filter('numberorcurrency', [
            '$filter', function ($filter) {
                return function (input, format, symbol, fractionSize) {
                    symbol = symbol || "$";
                    fractionSize = fractionSize || 2;

                    if (format == 'C') {
                        return $filter('currency')(input, symbol, fractionSize);
                    }

                    return $filter('number')(input, fractionSize);
                };
            }
        ])
        // Based on https://github.com/sparkalow/angular-truncate/blob/master/src/truncate.js
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

    angular.module('tubular.directives').directive('tbGrid', [
            'tubularHttp', '$filter', function(tubularHttp, $filter) {
                return {
                    template: '<div class="tubular-grid" ng-transclude></div>',
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
                        requireAuthentication: '@?',
                        name: '@?gridName'
                    },
                    controller: [
                        '$scope', 'localStorageService', 'tubularPopupService', 'tubularModel',
                        function($scope, localStorageService, tubularPopupService, TubularModel) {
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
                            $scope.search = {
                                Argument: '',
                                Operator: 'None'
                            };
                            $scope.isEmpty = false;
                            $scope.tempRow = new TubularModel($scope, {});
                            $scope.gridDataService = $scope.gridDataService || tubularHttp;
                            $scope.requireAuthentication = $scope.requireAuthentication || true;
                            $scope.name = $scope.name || 'tbgrid';
                            $scope.canSaveState = false;
                            $scope.groupBy = '';

                            $scope.$watch('columns', function (val) {
                                if ($scope.hasColumnsDefinitions === false || $scope.canSaveState === false)
                                    return;

                                localStorageService.set($scope.name + "_columns", $scope.columns);
                            }, true);

                            $scope.addColumn = function (item) {
                                if (item.Name === null) return;

                                if ($scope.hasColumnsDefinitions !== false)
                                    throw 'Cannot define more columns. Column definitions have been sealed';

                                $scope.columns.push(item);
                            };

                            $scope.createRow = function() {
                                var request = {
                                    serverUrl: $scope.serverSaveUrl,
                                    requestMethod: $scope.serverSaveMethod,
                                    timeout: $scope.requestTimeout,
                                    requireAuthentication: $scope.requireAuthentication,
                                    data: $scope.tempRow
                                };

                                $scope.currentRequest = $scope.gridDataService.retrieveDataAsync(request);

                                $scope.currentRequest.promise.then(
                                    function(data) {
                                        $scope.$emit('tbGrid_OnSuccessfulUpdate', data);
                                        $scope.tempRow.$isEditing = false;
                                    }, function(error) {
                                        $scope.$emit('tbGrid_OnConnectionError', error);
                                    }).then(function() {
                                        $scope.currentRequest = null;
                                        $scope.retrieveData();
                                    });
                            };

                            $scope.newRow = function() {
                                $scope.tempRow = new TubularModel($scope, {});
                                $scope.tempRow.$isEditing = true;
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

                            $scope.updateRow = function(row, externalSave) {
                                row.$isLoading = true;

                                var request = {
                                    serverUrl: $scope.serverSaveUrl,
                                    requestMethod: 'PUT',
                                    timeout: $scope.requestTimeout,
                                    requireAuthentication: $scope.requireAuthentication,
                                };

                                var requestObj = $scope.gridDataService.saveDataAsync(row, request);

                                if (externalSave == false)
                                    $scope.currentRequest = requestObj;

                                requestObj.promise.then(
                                        function(data) {
                                            $scope.$emit('tbGrid_OnSuccessfulUpdate', data);
                                        }, function(error) {
                                            $scope.$emit('tbGrid_OnConnectionError', error);
                                        })
                                    .then(function() {
                                        row.$isLoading = false;
                                        $scope.currentRequest = null;
                                    });
                            };

                            $scope.verifyColumns = function () {
                                var columns = localStorageService.get($scope.name + "_columns");
                                if (columns === null || columns === "") {
                                    // Nothing in settings, saving initial state
                                    localStorageService.set($scope.name + "_columns", $scope.columns);
                                    return;
                                }

                                for (var index in columns) {
                                    var columnName = columns[index].Name;
                                    var filtered = $scope.columns.filter(function (el) { return el.Name == columnName; });

                                    if (filtered.length == 0) continue;

                                    var current = filtered[0];
                                    // Updates visibility by now
                                    current.Visible = columns[index].Visible;
                                    // TODO: Restore filters
                                }
                            };

                            $scope.retrieveData = function () {
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
                                            var model = new TubularModel($scope, el);

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
                                angular.forEach($scope.columns, function (column) {
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

                                angular.forEach($scope.columns, function (column) {
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
                                            Argument: '',
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
        .directive('tbGridPager', [
            '$timeout', function($timeout) {
                return {
                    require: '^tbGrid',
                    template:
                        '<div class="tubular-pager">' +
                            '<pagination ng-disabled="$component.isEmpty" direction-links="true" boundary-links="true" total-items="$component.filteredRecordCount"' +
                            'items-per-page="$component.pageSize" max-size="5" ng-model="pagerPageNumber" ng-change="pagerPageChanged()">' +
                            '</pagination>' +
                            '<div>',
                    restrict: 'E',
                    replace: true,
                    transclude: false,
                    scope: true,
                    terminal: false,
                    controller: [
                        '$scope', '$element', function($scope, $element) {
                            $scope.$component = $scope.$parent.$parent;
                            $scope.tubularDirective = 'tubular-grid-pager';

                            $scope.$component.$watch('currentPage', function(value) {
                                $scope.pagerPageNumber = value;
                            });

                            $scope.pagerPageChanged = function() {
                                $scope.$component.requestedPage = $scope.pagerPageNumber;
                                var allLinks = $element.find('li a');
                                $(allLinks).blur();
                            };
                        }
                    ],
                    compile: function compile(cElement, cAttrs) {
                        return {
                            pre: function(scope, lElement, lAttrs, lController, lTransclude) {},
                            post: function(scope, lElement, lAttrs, lController, lTransclude) {
                                scope.firstButtonClass = lAttrs.firstButtonClass || 'fa fa-fast-backward';
                                scope.prevButtonClass = lAttrs.prevButtonClass || 'fa fa-backward';

                                scope.nextButtonClass = lAttrs.nextButtonClass || 'fa fa-forward';
                                scope.lastButtonClass = lAttrs.lastButtonClass || 'fa fa-fast-forward';

                                $timeout(function() {
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
        .directive('tbColumnHeader', [
            '$timeout', 'tubularConst', function($timeout, tubularConst) {

                return {
                    require: '^tbColumn',
                    template: '<a title="Click to sort. Press Ctrl to sort by multiple columns" class="column-header" ng-transclude href="javascript:void(0)" ng-click="sortColumn($event)"></a>',
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
                                    if (scope.$parent.column.SortDirection == 'Ascending')
                                        cssClass = tubularConst.upCssClass;

                                    if (scope.$parent.column.SortDirection == 'Descending')
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
                        '$scope', function ($scope) {
                            $scope.$component = $scope.$parent.$component || $scope.$parent.$parent.$component;
                            $scope.tubularDirective = 'tubular-row-set';
                        }
                    ]
                };
            }
        ])
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
                            $scope.selectableBool = $scope.selectable != "false";
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
                        '$scope', function ($scope) {
                            $scope.column = { Visible: true };
                            $scope.columnName = $scope.columnName || null;
                            $scope.$component = $scope.$parent.$parent.$component;

                            if ($scope.columnName != null) {
                                var columnModel = $scope.$component.columns
                                    .filter(function (el) { return el.Name == $scope.columnName; });

                                if (columnModel.length > 0) {
                                    $scope.column = columnModel[0];
                                }
                            }
                        }
                    ]
                };
            }
        ])
        .directive('tbGridPagerInfo', [
            function() {
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
                        '$scope', function($scope) {
                            $scope.$component = $scope.$parent.$parent;
                            $scope.fixCurrentTop = function() {
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

                            $scope.$component.$watch('filteredRecordCount', function() {
                                $scope.filtered = $scope.$component.totalRecordCount != $scope.$component.filteredRecordCount;
                                $scope.fixCurrentTop();
                            });

                            $scope.$component.$watch('currentPage', function() {
                                $scope.fixCurrentTop();
                            });

                            $scope.$component.$watch('pageSize', function() {
                                $scope.fixCurrentTop();
                            });

                            $scope.fixCurrentTop();
                        }
                    ]
                };
            }
        ])
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
        ]).directive('tbRowGroupHeader', [
            function () {

                return {
                    require: '^tbRowTemplate',
                    template: '<td class="row-group" colspan="{{group[0].$count}}"><ng-transclude></ng-transclude></td>',
                    restrict: 'E',
                    replace: true,
                    transclude: true,
                    scope: {
                        group: '='
                    }
                };
            }
        ]);
})();

///#source 1 1 tubular/tubular-directives-grid.js
(function() {
    'use strict';

    angular.module('tubular.directives').directive('tbTextSearch', [
        function() {
            return {
                require: '^tbGrid',
                template:
                    '<div>' +
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
                scope: {
                    css: '@'
                },
                terminal: false,
                controller: [
                    '$scope', function($scope) {
                        $scope.$component = $scope.$parent.$parent;
                        $scope.tubularDirective = 'tubular-grid-text-search';
                        $scope.lastSearch = "";

                        $scope.$watch("$component.search.Text", function(val) {
                            if (angular.isUndefined(val)) return;

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
    ]).directive('tbRemoveButton', [
        '$compile', function($compile) {

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
                    '$scope', '$element', function ($scope, $element) {
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
    ]).directive('tbSaveButton', [
        function() {

            return {
                require: '^tbGrid',
                template: '<div><button ng-click="save()" class="btn btn-default {{ saveCss || \'\' }}" ' +
                    'ng-disabled="!model.$valid()" ng-show="model.$isEditing">' +
                    '{{ saveCaption || \'Save\' }}' +
                    '</button>' +
                    '<button ng-click="cancel()" class="btn {{ cancelCss || \'btn-default\' }}" ng-show="model.$isEditing">' +
                    '{{ cancelCaption || \'Cancel\' }}' +
                    '</button></div>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    model: '=',
                    isNew: '=?',
                    component: '=?',
                    saveCaption: '@',
                    saveCss: '@',
                    cancelCaption: '@',
                    cancelCss: '@'
                },
                controller: [
                    '$scope', function($scope) {
                        $scope.isNew = $scope.isNew || false;

                        if ($scope.isNew) {
                            if (angular.isUndefined($scope.component))
                                throw 'Define component.';
                        }

                        $scope.save = function() {
                            if ($scope.isNew) {
                                $scope.component.createRow();
                            } else {
                                $scope.model.edit();
                            }
                        };

                        $scope.cancel = function() {
                            $scope.model.revertChanges();
                        };
                    }
                ]
            };
        }
    ]).directive('tbEditButton', [
        function() {

            return {
                require: '^tbGrid',
                template: '<button ng-click="model.edit()" class="btn btn-default {{ css || \'\' }}" ' +
                    'ng-hide="model.$isEditing">{{ caption || \'Edit\' }}</button>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    model: '=',
                    caption: '@',
                    css: '@'
                }
            };
        }
    ]).directive('tbPageSizeSelector', [
        function () {

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
    ]).directive('tbExportButton', [
        function() {

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
    ]).directive('tbPrintButton', [
        function() {

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
///#source 1 1 tubular/tubular-directives-editors.js
(function() {
    'use strict';

    angular.module('tubular.directives').directive('tbSimpleEditor', [
        'tubularEditorService',
        function(tubularEditorService) {

            return {
                template: '<div ng-class="{ \'form-group\' : isEditing, \'has-error\' : !$valid }">' +
                    '<span ng-hide="isEditing">{{value}}</span>' +
                    '<label ng-show="showLabel">{{ label }}</label>' +
                    '<input type="{{editorType}}" placeholder="{{placeholder}}" ng-show="isEditing" ng-model="value" class="form-control" ' +
                    ' ng-required="required" ng-readonly="readOnly" />' +
                    '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">{{error}}</span>' +
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
    ]).directive('tbNumericEditor', [
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
    ]).directive('tbDateTimeEditor', [
        'tubularEditorService', function(tubularEditorService) {

            return {
                template: '<div ng-class="{ \'form-group\' : isEditing }">' +
                    '<span ng-hide="isEditing">{{ value | date: format }}</span>' +
                    '<label ng-show="showLabel">{{ label }}</label>' +
                    '<input type="datetime-local" ng-show="isEditing" ng-model="value" class="form-control" ' +
                    'ng-required="required" ng-readonly="readOnly" />' +
                    '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">{{error}}</span>' +
                    '</div>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: tubularEditorService.defaultScope,
                controller: [
                    '$scope', function($scope) {
                        $scope.DataType = "numeric";

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
    ]).directive('tbDateEditor', [
        'tubularEditorService', function(tubularEditorService) {

            return {
                template: '<div ng-class="{ \'form-group\' : isEditing }">' +
                    '<span ng-hide="isEditing">{{ value | date: format }}</span>' +
                    '<label ng-show="showLabel">{{ label }}</label>' +
                    '<input type="date" ng-show="isEditing" ng-model="value" class="form-control" ' +
                    'ng-required="required" ng-readonly="readOnly" />' +
                    '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">{{error}}</span>' +
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
    ]).directive('tbDropdownEditor', [
        'tubularEditorService', 'tubularHttp', function(tubularEditorService, tubularHttp) {

            return {
                template: '<div ng-class="{ \'form-group\' : isEditing, \'has-error\' : !$valid }">' +
                    '<span ng-hide="isEditing">{{ value }}</span>' +
                    '<label ng-show="showLabel">{{ label }}</label>' +
                    '<select ng-options="d for d in options" ng-show="isEditing" ng-model="value" class="form-control" ' +
                    'ng-required="required" />' +
                    '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">{{error}}</span>' +
                    '</div>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: angular.extend({ options: '=?', optionsUrl: '@' }, tubularEditorService.defaultScope),
                controller: [
                    '$scope', function($scope) {
                        tubularEditorService.setupScope($scope);
                        $scope.$editorType = 'select';
                        $scope.dataIsLoaded = false;

                        $scope.loadData = function() {
                            if ($scope.dataIsLoaded) return;

                            var currentRequest = tubularHttp.retrieveDataAsync({
                                serverUrl: $scope.optionsUrl,
                                requestMethod: 'GET' // TODO: RequestMethod
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
    ]).directive('tbTypeaheadEditor', [
        'tubularEditorService', 'tubularHttp', '$q', function (tubularEditorService, tubularHttp, $q) {

            return {
                template: '<div ng-class="{ \'form-group\' : isEditing, \'has-error\' : !$valid }">' +
                    '<span ng-hide="isEditing">{{ value }}</span>' +
                    '<label ng-show="showLabel">{{ label }}</label>' +
                    '<input ng-show="isEditing" ng-model="value" class="form-control" typeahead="o for o in getValues($viewValue)" ' +
                    'ng-required="required" />' +
                    '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">{{error}}</span>' +
                    '</div>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: angular.extend({ options: '=?', optionsUrl: '@' }, tubularEditorService.defaultScope),
                controller: [
                    '$scope', function($scope) {
                        tubularEditorService.setupScope($scope);
                        $scope.$editorType = 'select';

                        $scope.getValues = function(val) {
                            if (angular.isDefined($scope.optionsUrl)) {
                                return tubularHttp.retrieveDataAsync({
                                    serverUrl: $scope.optionsUrl + '?search=' + val,
                                    requestMethod: 'GET' // TODO: RequestMethod
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
    ]).directive('tbHiddenField', [
        'tubularEditorService',
        function(tubularEditorService) {

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
    ]).directive('tbCheckboxField', [
        'tubularEditorService',
        function(tubularEditorService) {

            return {
                template: '<div ng-class="{ \'form-group\' : isEditing, \'has-error\' : !$valid }">' +
                    '<span ng-hide="isEditing">{{value}}</span>' +
                    '<label>' +
                    '<input type="checkbox" ng-show="isEditing" ng-model="value" ng-required="required" /> ' +
                    '<span ng-show="showLabel">{{label}}</span>' +
                    '</label>' +
                    '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">{{error}}</span>' +
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
    ]).directive('tbTextArea', [
        'tubularEditorService',
        function(tubularEditorService) {

            return {
                template: '<div ng-class="{ \'form-group\' : isEditing, \'has-error\' : !$valid }">' +
                    '<span ng-hide="isEditing">{{value}}</span>' +
                    '<label ng-show="showLabel">{{ label }}</label>' +
                    '<textarea ng-show="isEditing" placeholder="{{placeholder}}" ng-model="value" class="form-control" ' +
                    ' ng-required="required" ng-readonly="readOnly"></textarea>' +
                    '<span class="help-block error-block" ng-show="isEditing" ng-repeat="error in state.$errors">{{error}}</span>' +
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
        .directive('tbColumnFilterColumnSelector', [function() {
            return {
                template: '<div><hr /><h4>Columns Selector</h4><button class="btn btn-sm btn-default" ng-click="openColumnsSelector()">Select Columns</button></div>',
                restrict: 'E',
                replace: true,
                transclude: true,
            };
        }])
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

    angular.module('tubular.directives').directive('tbForm',
    ['tubularHttp', function(tubularHttp) {
            return {
                template: '<form ng-transclude></form>',
                restrict: 'E',
                replace: true,
                transclude: true,
                scope: {
                    rowModel: '=?',
                    serverUrl: '@',
                    serverSaveUrl: '@',
                    isNew: '@'
                },
                controller: [
                    '$scope', '$routeParams', '$location', 'tubularModel', function ($scope, $routeParams, $location, TubularModel) {
                        $scope.tubularDirective = 'tubular-form';
                        $scope.fields = [];
                        $scope.hasFieldsDefinitions = false;
                        $scope.modelKey = $routeParams.param;
                        
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
                            angular.forEach($scope.fields, function (column) {
                                column.$parent.Model = $scope.rowModel;

                                // TODO: this behavior is nice, but I don't know how to apply to inline editors
                                if (column.$editorType == 'input' &&
                                    angular.equals(column.value, $scope.rowModel[column.Name]) == false) {
                                    column.value = $scope.rowModel[column.Name];

                                    $scope.$watch(function() {
                                        return column.value;
                                    }, function(value) {
                                        $scope.rowModel[column.Name] = value;
                                    });
                                }

                                // Ignores models without state
                                if (angular.isUndefined($scope.rowModel.$state)) return;

                                if (angular.equals(column.state, $scope.rowModel.$state[column.Name]) == false) {
                                    column.state = $scope.rowModel.$state[column.Name];
                                }
                            });
                        };

                        $scope.retrieveData = function() {
                            if (angular.isUndefined($scope.serverUrl)) {
                                if (angular.isUndefined($scope.rowModel)) {
                                    $scope.rowModel = new TubularModel($scope, {});
                                }

                                $scope.bindFields();

                                return;
                            }

                            tubularHttp.get($scope.serverUrl + $scope.modelKey).promise.then(
                                function(data) {
                                    $scope.rowModel = new TubularModel($scope, data);

                                    $scope.bindFields();
                                }, function(error) {
                                    $scope.$emit('tbGrid_OnConnectionError', error);
                                });
                        };

                        $scope.updateRow = function(row) {
                            var request = {
                                serverUrl: $scope.serverSaveUrl,
                                requestMethod: 'PUT'
                            };

                            $scope.currentRequest = tubularHttp.saveDataAsync(row, request);

                            $scope.currentRequest.promise.then(
                                    function(data) {
                                        $scope.$emit('tbGrid_OnSuccessfulUpdate', data);
                                        $scope.$emit('tGrid_OnSuccessfulForm', data);
                                    }, function(error) {
                                        $scope.$emit('tbGrid_OnConnectionError', error);
                                    })
                                .then(function() {
                                    $scope.currentRequest = null;
                                });
                        };

                        $scope.create = function () {
                            // TODO: Method could be PUT?
                            $scope.currentRequest = tubularHttp.post($scope.serverSaveUrl, $scope.rowModel).promise.then(
                                    function(data) {
                                        $scope.$emit('tbGrid_OnSuccessfulUpdate', data);
                                        $scope.$emit('tGrid_OnSuccessfulForm', data);
                                    }, function(error) {
                                        $scope.$emit('tbGrid_OnConnectionError', error);
                                    })
                                .then(function() {
                                    $scope.currentRequest = null;
                                });
                        };

                        $scope.save = function () {
                            // TODO: Why Save and Create is not the same?ñ
                            if ($scope.rowModel.save() === false) {
                                $scope.$emit('tbGrid_OnSavingNoChanges', $scope.rowModel);
                            }
                        };

                        $scope.gotoView = function(view) {
                            $location.path(view);
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
        
        angular.module('tubular.models', [])
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
            .factory('tubulargGridFilterModel', function() {

                return function(attrs) {
                    this.Text = attrs.text || null;
                    this.Argument = attrs.argument || null;
                    this.Operator = attrs.operator || null;
                    this.OptionsUrl = attrs.optionsUrl || null;
                };
            })
            .factory('tubularModel', [
                '$timeout', '$location', function ($timeout, $location) {
                    return function ($scope, data) {
                        var obj = {
                            $key: "",
                            $count: 0,
                            $addField: function (key, value) {
                                this.$count++;

                                this[key] = value;
                                if (angular.isUndefined(this.$original)) this.$original = {};
                                this.$original[key] = value;

                                if (angular.isUndefined(this.$state)) this.$state = {};
                                this.$state[key] = {
                                    $valid: function() {
                                        return this.$errors.length == 0;
                                    },
                                    $errors: []
                                };

                                $scope.$watch(function () {
                                    return obj[key];
                                }, function (newValue, oldValue) {
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
                            angular.forEach($scope.columns, function (col, key) {
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

                        obj.$valid = function () {
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

                        obj.save = function (externalSave) {
                            return obj.$hasChanges ? $scope.updateRow(obj, externalSave) : false;
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

    angular.module('tubular.services', ['ui.bootstrap'])
        .service('tubularPopupService', [
            '$modal', function tubularPopupService($modal) {
                var me = this;

                me.openDialog = function(template, model) {
                    var dialog = $modal.open({
                        templateUrl: template,
                        backdropClass: 'fullHeight',
                        controller: [
                            '$scope', function($scope) {
                                $scope.Model = model;

                                $scope.savePopup = function () {
                                    $scope.Model.save(true);
                                    
                                    $scope.$on('tbGrid_OnSuccessfulUpdate', 
                                        function () {
                                            // TODO: Review this
                                            dialog.close();
                                        });
                                };

                                $scope.closePopup = function () {
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
        .service('tubularGridExportService', function tubularGridExportService() {
            var me = this;
            
            me.getColumns = function (gridScope) {
                return gridScope.columns
                    .filter(function (c) { return c.Visible; })
                    .map(function (c) { return c.Name.replace(/([a-z])([A-Z])/g, '$1 $2'); });
            };

            me.getColumnsVisibility = function (gridScope) {
                return gridScope.columns
                    .map(function (c) { return c.Visible; });
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

                var blob = new Blob([csvFile], { type: 'text/csv;charset=utf-8;' });
                saveAs(blob, filename);
            };
        })
        .service('tubularGridFilterService', [
            'tubulargGridFilterModel', '$compile', '$modal', function tubularGridFilterService(FilterModel, $compile, $modal) {
                var me = this;

                me.applyFilterFuncs = function (scope, el, attributes, openCallback) {
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

                    scope.openColumnsSelector = function () {
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
                                '$scope', function ($innerScope) {
                                    $innerScope.Model = model;

                                    $innerScope.closePopup = function () {
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
                    }

                    if (scope.dataType === 'numeric') {
                        scope.filter.Argument = [1];
                    }

                    scope.filterTitle = lAttrs.title || "Filter";
                };
            }
        ])
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
                    format: '=?',
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
                                    return this.$errors == 0;
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

    // User Service based on https://bitbucket.org/david.antaramian/so-21662778-spa-authentication-example
    angular.module('tubular.services').service('tubularHttp', [
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
                if (typeof savedData === 'undefined') {
                    throw new Exception('No authentication data exists');
                } else if (isAuthenticationExpired(savedData.expirationDate)) {
                    throw new Exception('Authentication token has already expired');
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
                }, function(error) {
                    if (angular.isDefined(error) && angular.isDefined(error.status) && error.status == 401) {
                        if (me.isAuthenticated()) {
                            me.removeAuthentication();
                            // Let's trigger a refresh
                            document.location = document.location;
                        }
                    }
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

            me.get = function(url) {
                return me.retrieveDataAsync({
                    serverUrl: url,
                    requestMethod: 'GET',
                });
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
        }
    ]);
})();
///#source 1 1 tubular/tubular-services-odata.js
(function () {
    'use strict';

    angular.module('tubular.services')
        .service('tubularOData', [
            'tubularHttp', function tubularOData(tubularHttp) {
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

                    url += "&$select=" + params.Columns.map(function(el) { return el.Name; }).join(',');
                    url += "&$skip=" + params.Skip;
                    url += "&$top=" + params.Take;

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
                    tubularHttp.saveDataAsync(model, request); //TODO: Check how to handle
                };

                me.get = function(url) {
                    tubularHttp.get(url);
                };

                me.delete = function(url) {
                    tubularHttp.delete(url);
                };

                me.post = function(url, data) {
                    tubularHttp.post(url, data);
                };

                me.put = function(url, data) {
                    tubularHttp.put(url, data);
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
