var express = require('express');
var router = express.Router();

var request = require('request');
var storage = require('azure-storage');

var LINQ = require('node-linq').LINQ;

var azuremgt = require('./azuremgt');

var _config = require('./config.js');

/* GET home page. */
router.get('/', function (req, res, next) {

    res.render('index', {
        toptitle: _config.serviceTitle, blobname: _config.getBlobAcctName(_config.connectionString),
        readonly: _config.serviceReadOnly
    });

});

router.get('/debug', function (req, res, next) {

    var token = req.cookies.token;
    var retoken = req.cookies.refresh_token;

    res.render('output', { token: token, retoken: retoken });

});


router.get('/search', function (req, res, next) {

    var page = req.query.page;
    var keyword = req.query.keyword ? req.query.keyword : "*";

    if (!page || page <= 0 ) { page = 1; }

    azuremgt.getSearchResult(page, keyword, function (err, resp, results, pcount, count, tags) {
        if (err || resp.statusCode != 200) {
            res.render('error', { error: err, toptitle: _config.serviceTitle });
        } else {
            res.render('search', {
                toptitle: _config.serviceTitle, title: "Image Search",
                results: results, page: page, pagecount: pcount, keyword: keyword, count: count,
                readonly: _config.serviceReadOnly,
                issecondary: _config.isSecondary,
                tags: tags
            });
        }
    });

});

router.post('/search', function (req, res, next) {

    console.log("post '/image',", encodeURIComponent(req.body.keyword));

    var keyword = req.body.keyword ? req.body.keyword : "*";
    var page = 1;

    azuremgt.getSearch(keyword, function (err, resp, results, pcount, count, tags) {
        if (err || resp.statusCode != 200) {
            res.render('error', { error: err, toptitle: _config.serviceTitle });
        } else {
            res.render('search', {
                toptitle: _config.serviceTitle, title: "Image Search",
                results: results, page: page, pagecount: pcount, keyword: keyword, count: count,
                readonly: _config.serviceReadOnly,
                issecondary: _config.isSecondary,
                tags: tags
            });
        }
    });

});

router.post('/search/edit', function (req, res, next) {

    console.log("post '/search/edit'");

    azuremgt.editSearch(req.body.fotoid, req.body.fotocaption, function (err, resp, body) {
        if (err || resp.statusCode != 200) {
            res.render('error', { error: err, toptitle: _config.serviceTitle });
        } else {
            res.redirect('/search');
        }
    });

});

router.post('/search/delete', function (req, res, next) {

    console.log("post '/search/delte'");

    azuremgt.deleteSearch(req.body.deleteid, function (err, resp, body) {
        if (err || resp.statusCode != 200) {
            res.render('error', { error: err, toptitle: _config.serviceTitle });
        } else {
            res.redirect('/search');
        }
    });

});


router.get('/ping', function (req, res, next) {

    azuremgt.pingHealth(res);
});

router.get('/upload', function (req, res, next) {

    res.render('upload', { toptitle: _config.serviceTitle, title: "Image Search", readonly: _config.serviceReadOnly });

});

router.get('/sasurl', function (req, res) {

    var cn = _config.connectionString;
    var blobService = storage.createBlobService(cn);

    var blockBlobName = req.query.blobName;
    var containerName = req.query.containerName ? req.query.containerName : _config.containerName;

    var expiryDate = new Date();
    expiryDate.setMinutes(expiryDate.getMinutes() + 30);

    var sharedAccessPolicy = {
        AccessPolicy: {
            Permissions: storage.BlobUtilities.SharedAccessPermissions.READ + storage.BlobUtilities.SharedAccessPermissions.WRITE + storage.BlobUtilities.SharedAccessPermissions.LIST,
            Expiry: expiryDate
        },
    };

    var sas = blobService.generateSharedAccessSignature(containerName, blockBlobName, sharedAccessPolicy);
    var sasUrl = blobService.getUrl(containerName, blockBlobName, sas);

    res.send(sasUrl);

});

router.post('/addqueue', function (req, res, next) {

    console.log("post '/addqueue',", req.body.filename);

    var queueService = storage.createQueueService(_config.connectionString);
    queueService.messageEncoder = new storage.QueueMessageEncoder.TextBase64QueueMessageEncoder();

    queueService.createMessage("azf-blobtrigger", req.body.filename, function (err) {
        if (!err) {
            console.log("queue success");
            res.send('queue success');
        } else {
            res.send(err.statusCode, err.message);
        }
    });
});

router.get('/log', function (req, res, next) {

    var table = storage.createTableService(_config.connectionString);

    var query = new storage.TableQuery(); //.top(10);

    table.queryEntities('fotoslog', query, null, function (error, result, response) {
        if (!error) {
            // query was successful

            //NOTE: table query does not support orderby                
            var body = new LINQ(response.body.value)
                .Reverse()
                //.OrderByDescending(function (item) { return [item.Timestamp]; })
                .ToArray();

            res.render('log', { title: "Recent Job Logs", body: body });
        }
    });
});

router.get('/config', function (req, res, next) {

    res.render('config', { config: JSON.stringify(_config, null, 4) });
});

module.exports = router;

