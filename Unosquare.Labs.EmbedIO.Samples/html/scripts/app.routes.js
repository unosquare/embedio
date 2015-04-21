(function () {
    angular.module('app.routes', ['app.constants', 'ngRoute'])

        .config(['$routeProvider', '$locationProvider', function($routeProvider, $locationProvider) {
            $routeProvider.
                when('/', {
                    templateUrl: '/views/home.html',
                    title: 'Home'
                }).
                when('/people', {
                    templateUrl: '/views/people.html',
                    title: 'People'
                }).
                when('/chat', {
                    templateUrl: '/views/chat.html',
                    title: 'Chat Room'
                }).
                when('/cmd', {
                    templateUrl: '/views/cmd.html',
                    title: 'Command-Line Interface'
                }).
                when('/tubular', {
                    templateUrl: '/views/tubular.html',
                    title: 'Tubular Grid'
                }).
                otherwise({
                    redirectTo: '/'
                });
        
            //$locationProvider.html5Mode(true);
        }]);
})();