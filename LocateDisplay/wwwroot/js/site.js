// Write your JavaScript code.
$(".hiddenRow").on("click", function (e) {
    var prev = e.currentTarget.parentElement.previousElementSibling;
    prev.click();
});