var dropZone = document.getElementById('dropZone');
var dropMark = document.getElementById('dropMark');

function updateProgression(labelFiles, labelPercent, progressBar, current, max) {
    if (labelFiles) {
        labelFiles.innerText = current + '/' + max + ' ';
    }
    if (labelPercent) {
        labelPercent.innerText = '(' + Math.round((current * 100) / max) + '%)';
    }
    if (progressBar) {
        progressBar.style.width = ((current * 100) / max) + '%';
    }
}

function queryVersions() {
    var xhr = new XMLHttpRequest();

    var lblClientVersion = document.getElementById('lblClientVersion');
    var lblServerVersion = document.getElementById('lblServerVersion');
    var lblEnvironment = document.getElementById('lblEnvironment');

    lblClientVersion.innerText = '?';
    lblServerVersion.innerText = '?';
    lblEnvironment.innerText = '?';

    xhr.open('GET', '/api/versions', true);

    xhr.timeout = 5000; // 5 seconds
    xhr.onload = function () {

        var parts;
        var clientVersion;
        var serverVersion;

        if (xhr.readyState !== 4) {
            return;
        }

        if (xhr.status >= 200 && xhr.status <= 299) {
            parts = xhr.responseText.split(';');
            if (parts.length === 3) {

                clientVersion = parseInt(parts[0], 10);
                serverVersion = parseInt(parts[1], 10);

                if (clientVersion > 0) {
                    lblClientVersion.innerText = clientVersion.toString();
                }

                if (serverVersion > 0) {
                    lblServerVersion.innerText = serverVersion.toString();
                }

                lblEnvironment.innerText = parts[2];

                return;
            }
        }

        lblClientVersion.innerText = 'X';
        lblServerVersion.innerText = 'X';
        lblEnvironment.innerText = 'X';
    };

    xhr.send();
}

function upload(file, statusCell, commentCell, progressBar, context) {

    var prevLoaded = 0;
    var xhr = new XMLHttpRequest();

    xhr.open('POST', '/api/upload', true);

    xhr.timeout = 0; // no timeout
    xhr.onload = function () {
        if (xhr.readyState !== 4) {
            return;
        }
        commentCell.innerText = xhr.responseText;
        if (xhr.status >= 200 && xhr.status <= 299) {
            statusCell.innerHTML = '<span class="successColor">OK</span>';
            context.success();
        } else {
            statusCell.innerHTML = '<span class="failureColor">FAILED (after transfer started)</span>';
            context.failed();
        }
    };

    xhr.upload.onprogress = function (e) {
        if (e.lengthComputable) {
            updateProgression(null, null, progressBar, e.loaded, e.total);
            context.updateTotalProgression(e.loaded - prevLoaded);
            prevLoaded = e.loaded;
        }
    };

    xhr.onerror = function (e) {
        statusCell.innerHTML = '<span class="failureColor">FAILED (before transfer started)</span>';
        context.failed();
    };

    var formData = new FormData();
    formData.append('files', file, file.name);
    // formData.append('lastModified', file.lastModified);

    xhr.send(formData);
}

dropZone.addEventListener('dragover', function(e) {
    e.stopPropagation();
    e.preventDefault();
    e.dataTransfer.dropEffect = 'copy';
});

var htmlReallySucksCounter = 0;

dropZone.addEventListener('dragenter', function (e) {
    htmlReallySucksCounter += 1;
    e.stopPropagation();
    e.preventDefault();
    if (htmlReallySucksCounter === 1) {
        dropMark.style.display = 'initial';
    }
});

dropZone.addEventListener('dragleave', function (e) {
    htmlReallySucksCounter -= 1;
    e.stopPropagation();
    e.preventDefault();
    if (htmlReallySucksCounter === 0) {
        dropMark.style.display = 'none';
    }
});

dropZone.addEventListener('drop', function(e) {
    var i;
    var file;
    var files;

    var item;
    var entry;

    var successFiles = -1;
    var failedFiles = -1;

    var totalFiles;
    var totalBytes = 0;
    var currentTotalBytes = 0;

    var context;

    var reportTable;
    var row;
    var filenameCell;
    var statusCell;

    var lblSuccessFiles;
    var lblSuccessPercent;

    var lblFailedFiles;
    var lblFailedPercent;

    var lblTotalFiles;
    var lblTotalPercent;

    var totalProgressBar;

    e.stopPropagation();
    e.preventDefault();

    htmlReallySucksCounter = 0;
    dropMark.style.display = 'none';

    files = [];

    for (item of e.dataTransfer.items) {
        entry = item.webkitGetAsEntry();
        if (entry.isFile) {
            files.push(item.getAsFile());
        }
    }

    totalFiles = files.length;

    if (totalFiles === 0) {
        return;
    }

    files.sort(function (a, b) { return a.name < b.name ? -1 : +1; });

    lblSuccessFiles = document.getElementById('lblSuccessFiles');
    lblSuccessPercent = document.getElementById('lblSuccessPercent');

    lblFailedFiles = document.getElementById('lblFailedFiles');
    lblFailedPercent = document.getElementById('lblFailedPercent');

    lblTotalFiles = document.getElementById('lblTotalFiles');
    lblTotalPercent = document.getElementById('lblTotalPercent');

    totalProgressBar = document.getElementById('totalProgressBar');

    context = {
        updateTotalProgression: function (deltaLoadedSize) {
            currentTotalBytes += deltaLoadedSize;
            updateProgression(null, lblTotalPercent, totalProgressBar, currentTotalBytes, totalBytes);
        },
        success: function () {
            successFiles += 1;
            updateProgression(lblSuccessFiles, lblSuccessPercent, null, successFiles, totalFiles);
            updateProgression(lblTotalFiles, null, null, successFiles + failedFiles, totalFiles);
        },
        failed: function () {
            failedFiles += 1;
            updateProgression(lblFailedFiles, lblFailedPercent, null, failedFiles, totalFiles);
            updateProgression(lblTotalFiles, null, null, successFiles + failedFiles, totalFiles);
        }
    };

    context.success();
    context.failed();

    reportTable = document.getElementById('reportTable');
    reportTable.innerHTML = '';

    row = document.createElement('tr');
    filenameCell = document.createElement('th');
    filenameCell.innerHTML = 'Filename';
    statusCell = document.createElement('th');
    statusCell.innerHTML = 'Status';
    commentCell = document.createElement('th');
    commentCell.innerHTML = 'Comment';
    row.appendChild(filenameCell);
    row.appendChild(statusCell);
    row.appendChild(commentCell);
    reportTable.appendChild(row);

    for (i = 0; i < files.length; i += 1) {
        file = files[i];

        totalBytes += file.size;

        row = document.createElement('tr');
        filenameCell = document.createElement('td');
        filenameCell.innerHTML = file.name;
        statusCell = document.createElement('td');
        commentCell = document.createElement('td');

        var progressBarContainer = document.createElement('div');
        progressBarContainer.className = 'progressBarContainer';

        var progressBar = document.createElement('div');
        progressBar.className = 'progressBar';
        progressBarContainer.appendChild(progressBar);

        statusCell.appendChild(progressBarContainer);

        row.appendChild(filenameCell);
        row.appendChild(statusCell);
        row.appendChild(commentCell);
        reportTable.appendChild(row);

        upload(file, statusCell, commentCell, progressBar, context);
    }
});

queryVersions();
