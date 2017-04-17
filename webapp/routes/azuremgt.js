/*
 * Azure Management Module
 * 
 */

var async = require('async');
var request = require('request');
var _config = require('./config.js');

module.exports = {
    getSearchResult: function (page, keyword, cb) {

        var skip = _config.pagesize * (page - 1);
        var config = {
            uri: 'https://' + _config.searchAccount + '.search.windows.net/indexes/' + _config.searchIndex + '/docs?api-version=2015-02-28&$top=' + _config.pagesize + '&$skip=' + skip + '&facet=tags&$count=true&search=' + encodeURIComponent(keyword),
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'api-key': _config.searchApiKey
            },
            json: true
        };

        request(config, function (err, resp, body) {

            if (err) {
                return cb(err);
            }
            
            var facet = body['@search.facets'];
            var tags = (facet) ? facet.tags : null;            
            var results = body.value;

            results.forEach(function (element) {
                element.path = decodeURIComponent(element.path);
            }, this);

            var count = body['@odata.count'];
            var pcount = Math.ceil(count / _config.pagesize);

            cb(err, resp, results, pcount, count, tags);

        });
    },

    getSearch: function (keyword, cb) {

        var config = {
            uri: 'https://' + _config.searchAccount + '.search.windows.net/indexes/' + _config.searchIndex + '/docs?api-version=2015-02-28&$top=' + _config.pagesize + '&facet=tags&$count=true&search=' + encodeURIComponent(keyword),
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'api-key': _config.searchApiKey
            },
            json: true
        };

        request(config, function (err, resp, body) {

            if (err) {
                return cb(err);
            }

            var facet = body['@search.facets'];
            var tags = facet.tags;
            var results = body.value;

            results.forEach(function (element) {
                element.path = decodeURIComponent(element.path);
            }, this);

            var count = body['@odata.count'];
            var pcount = Math.ceil(count / _config.pagesize);

            cb(null, resp, results, pcount, count, tags);
        });
    },

    editSearch: function (id, caption, cb) {

        var payload = { "id": id, "caption": caption };
        payload['@search.action'] = 'merge';

        var config = {
            //uri: 'https://' + _config.searchAccount + '.search.windows.net/indexes/' + _config.searchIndex + '/docs/index?api-version=2016-09-01',
            uri: _config.apiAppUrl,
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'api-key': _config.searchApiKey
            },
            json: true,
            body: payload
        };

        request(config, function (err, resp, body) {

            cb(err, resp, body);

        });
    },

    deleteSearch: function (id, cb) {

        var payload = { "id": id, };
        payload['@search.action'] = 'delete';

        var config = {
            uri: _config.apiAppUrl,
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'api-key': _config.searchApiKey
            },
            json: true,
            body: payload
        };

        request(config, function (err, resp, body) {

            cb(err, resp, body);

        });
    },


    pingHealth: function (res) {

        var config_sch = {
            uri: 'https://' + _config.searchAccount + '.search.windows.net/indexes/' + _config.searchIndex + '/docs?api-version=2015-02-28&$top=0&search=null',
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'api-key': _config.searchApiKey
            },
            json: true
        };

        _config.storageAcctName = (_config.storageAcctName === "") ? _config.getBlobAcctName(_config.connectionString) : _config.storageAcctName;
        var config_str = {
            uri: 'https://' + _config.storageAcctName + '.blob.core.windows.net/images/health.json',
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        };

        async.series(
            [
                // check search health
                function (callback) {
                    request(config_sch, function (err, resp, body) {
                        if (err || resp.statusCode != 200) {
                            callback("error");
                        }
                        else {
                            callback(null, 200);
                        }
                    });
                },
                // check storage health
                function (callback) {
                    request(config_str, function (err, resp, body) {
                        if (err || resp.statusCode != 200) {
                            callback("error");
                        }
                        else {
                            callback(null, 200);
                        }
                    });
                }
            ],
            function (err, results) {
                if (err) {
                    console.log(err);
                    res.send(404);
                }
                else {
                    res.send(200);
                }
            });
    },

};