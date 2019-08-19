(function () {
    angular.module('app.routes', ['ngRoute'])

        .config(['$routeProvider', '$locationProvider', function($routeProvider) {
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
        }]);
})();