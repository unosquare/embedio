(function() {
    angular.module('app.directives', ['ngRoute'])
    
        .directive('appPerson', function() {
            return {
                restrict: 'E',
                templateUrl: '/partials/app-person.html',
                controller: function () {
                    var me = this;
                    me.showInfo = function(item) { 
                        alert(item.Name + ': My id is ' + item.Key + ' and my age is ' + item.Age); 
                    };
                },
                controllerAs: 'person'
            };
        })
        
        .directive('appMenu', function() {
            return {
                restrict: 'E',
                templateUrl: '/partials/app-menu.html',
                controller: ['$scope', '$route', '$location', function($scope, $route, $location) {
                    var me = this;
                    me.whereAmI = function () {
                        return $location.path(); 
                    };

                    me.isActive = function (path) {
                        return $location.path() === path;
                    };
                }],
                controllerAs: 'menu'
            };
        });
})();