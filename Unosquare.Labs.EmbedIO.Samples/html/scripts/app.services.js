(function () {
    angular.module('app.services', ['app.constants', 'ngRoute'])

        .service('PeopleApiService', ['$http', '$q', '$timeout', 'HttpTimeout', function PeopleApiService($http, $q, $timeout, HttpTimeout) {
            var me = this;
            me.getAllPeopleAsync = function () {

                var apiCall = $q.defer();
                var timeoutHanlder = $timeout(function () {
                    apiCall.reject('Timed out');
                }, HttpTimeout);

                $http.get('/api/people/').success(function (data) {
                    $timeout.cancel(timeoutHanlder);
                    apiCall.resolve(data);
                }).error(function (data) {
                    $timeout.cancel(timeoutHanlder);
                    apiCall.reject(data);
                });

                $timeout(function () { apiCall.notify('Retrieving Data . . .'); }, 0);
                return apiCall.promise;
            };
        }]);
})();