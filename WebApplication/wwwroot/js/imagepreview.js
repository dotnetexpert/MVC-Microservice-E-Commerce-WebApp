﻿function previewImage(e) { var a = e.target, t = new FileReader; t.onload = function () { var e = document.getElementById("imagePreview"), a = document.getElementById("imageUrl"); e.src = t.result, e.style.display = "block", a.value = t.result }, t.readAsDataURL(a.files[0]) }