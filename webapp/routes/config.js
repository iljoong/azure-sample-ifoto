// DEMO
module.exports = {

    // title
    "serviceTitle": process.env.FOTOS_TITLE || "iFOTOS",
    "serviceReadOnly": (process.env.FOTOS_READONLY === 'true') || false,
    "isSecondary": (process.env.FOTOS_ISSECONDARY === 'true') || false,
    "pagesize": process.env.FOTOS_PAGESIZE || 10,

    // storage
    "connectionString": process.env.FOTOS_STRCONN,
    "containerName": process.env.FOTOS_CONTAINER,

    // search api
    "searchAccount": process.env.FOTOS_SCHACCT,
    "searchApiKey": process.env.FOTOS_SCHAPIKEY,
    "searchIndex": process.env.FOTOS_SCHINDEX,

    apiAppUrl: process.env.FOTOS_APIAPPURL,

    storageAcctName: "",

    getBlobAcctName: function (connstr) {

        var accName = '';
        var connString = connstr.split(';');

        connString.forEach(function (element) {
            var _name = element.substr(0, 11);
            if ('AccountName' == _name) {
                accName = element.split('=')[1];
            }
        }, this);

        return accName;
    }
};

