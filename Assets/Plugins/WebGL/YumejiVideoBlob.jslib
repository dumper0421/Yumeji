// 게임 데이터에 들어 있는 영상 바이트를 브라우저 Blob URL로 바꾼다.
// Unity Play 같은 호스트는 StreamingAssets 폴더를 올려주지 않아서, 영상을 별도 파일 대신 데이터 안에 넣어 재생한다.
mergeInto(LibraryManager.library, {
  YumejiCreateVideoBlobUrl: function (ptr, length, mimePtr) {
    var mime = UTF8ToString(mimePtr);
    var blob = new Blob([HEAPU8.subarray(ptr, ptr + length)], { type: mime });
    var url = URL.createObjectURL(blob);
    var size = lengthBytesUTF8(url) + 1;
    var buffer = _malloc(size);
    stringToUTF8(url, buffer, size);
    return buffer;
  },
});
