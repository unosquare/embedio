(function() {
    angular.module('app.services', ['ngRoute'])
        .service('PeopleApiService', [
            '$http', '$q', '$timeout', function peopleApiService($http, $q, $timeout) {
                var me = this;

                me.getAllPeopleAsync = function() {

                    var apiCall = $q.defer();
                    var timeoutHanlder = $timeout(function() {
                        apiCall.reject('Timed out');
                    }, 2000);

                    $http.get('/api/people/').success(function(data) {
                        $timeout.cancel(timeoutHanlder);
                        apiCall.resolve(data);
                    }).error(function(data) {
                        $timeout.cancel(timeoutHanlder);
                        apiCall.reject(data);
                    });

                    $timeout(function() { apiCall.notify('Retrieving Data . . .'); }, 0);
                    return apiCall.promise;
                };
            }
        ]);
})();