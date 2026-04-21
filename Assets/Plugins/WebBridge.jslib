mergeInto(LibraryManager.library, {
  SaveScoreToWeb: function (score) {
    // Gửi tin nhắn lên website cha (React)
    window.parent.postMessage({
      type: 'SAVE_SCORE',
      payload: { score: score }
    }, '*');
    console.log("Game sent score to Web: " + score);
  },
});