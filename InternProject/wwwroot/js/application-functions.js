console.log("JS dosyası gerçekten yüklendi");

function sendApplicationForm() {
	const model = {
		projectName: $('#projectName').val(),
		applicantUnit: parseInt($('#applicantUnit').val()),
		appliedProject: parseInt($('#appliedProject').val()),
		appliedType: parseInt($('#appliedType').val()),
		participantType: parseInt($('#participantType').val()),
		applicationPeriod: parseInt($('#applicationPeriod').val()),
		applicationDate: $('#applicationDate').val(),
		applicationState: parseInt($('#applicationState').val()),
		stateDate: $('#stateDate').val(),
		grantAmount: $('#grantAmount').val()
	};

	$.ajax({
		type: 'POST',
		url: '/Home/FormSave',
		contentType: 'application/json',
		data: JSON.stringify(model),
		success: function (response) {
			if (response.success) {
				alert(response.message);
			} else {
				alert("Hata: " + response.message);
			}
		},
		error: function () {
			alert("Sunucu hatası oluştu.");
		}
	});
}

function loadDropdown(type, selectId, selectedId = null) {
	$.ajax({
		url: '/Home/GetDropdownOptions?type=' + encodeURIComponent(type),
		type: 'GET',
		success: function (data) {
			const $select = $(selectId);
			$select.empty();
			$select.append('<option value="">Seçiniz</option>');
			$.each(data, function (_, item) {
				const option = $('<option>', {
					value: item.id,
					text: item.subtype
				});
				if (selectedId && item.id == selectedId) {
					option.prop('selected', true);
				}
				$select.append(option);
			});
		},
		error: function () {
			console.error("Dropdown yüklenirken hata oluştu.");
		}
	});
}


function initStaticDropdown(selector) {
	const $select = $(selector);
	$select.empty();
	$select.append('<option value="">Seçiniz</option>');
	$select.append('<option value="1">Edit</option>');
	$select.append('<option value="2">Delete</option>');
}

$(document).ready(function () {
	loadDistinctTypes();
	loadDropdown('Başvuran Birim', '#applicantUnit');
	loadDropdown('Başvuru Yapılan Proje', '#appliedProject');
	loadDropdown('Başvuru Yapılan Tür', '#appliedType');
	loadDropdown('Katılımcı Türü', '#participantType');
	loadDropdown('Başvuru Dönemi', '#applicationPeriod');
	loadDropdown('Başvuru Durumu', '#applicationState');
});

let pageSize = 10;
let currentPage = 1;

function getApplicationsPaged(page = 1) {
	currentPage = page;
	const formData = $('#filterForm').serialize();
	$.ajax({
		type: 'GET',
		url: `/Home/GetApplicationsPaged?page=${page}&pageSize=${pageSize}&${formData}`,
		contentType: 'application/json',
		success: function (response) {
			renderTable(response.data);
			setupPagination(response.totalCount);
		},
		error: function () {
			alert("Veri çekme hatası.");
		}
	});
}

function applyFilter(page = 1) {
	const formData = $('#filterForm').serialize();
	$.ajax({
		type: 'GET',
		url: `/Home/GetApplicationsPaged?page=${page}&pageSize=${pageSize}&${formData}`,
		contentType: 'application/json',
		success: function (response) {
			renderTable(response.data);
			setupPagination(response.totalCount);
			$('#exportBtn').prop('disabled', false);
		},
		error: function () {
			alert("Veri çekme hatası.");
		}
	});
}

function clearFilter() {
	$('#filterForm').find("input, select").val('').prop('selectedIndex', 0);
}

function setupPagination(totalCount) {
	const totalPages = Math.ceil(totalCount / pageSize);
	const pagination = $('.pagination');
	pagination.empty();

	pagination.append(`
		<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
			<a class="page-link" href="#" onclick="getApplicationsPaged(1); return false;">
				<i class="tf-icon bx bx-chevrons-left"></i>
			</a>
		</li>
		<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
			<a class="page-link" href="#" onclick="getApplicationsPaged(${currentPage - 1}); return false;">
				<i class="tf-icon bx bx-chevron-left"></i>
			</a>
		</li>
	`);

	for (let i = 1; i <= totalPages; i++) {
		pagination.append(`
			<li class="page-item ${i === currentPage ? 'active' : ''}">
				<a class="page-link" href="#" onclick="getApplicationsPaged(${i}); return false;">${i}</a>
			</li>
		`);
	}

	pagination.append(`
		<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
			<a class="page-link" href="#" onclick="getApplicationsPaged(${currentPage + 1}); return false;">
				<i class="tf-icon bx bx-chevron-right"></i>
			</a>
		</li>
		<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
			<a class="page-link" href="#" onclick="getApplicationsPaged(${totalPages}); return false;">
				<i class="tf-icon bx bx-chevrons-right"></i>
			</a>
		</li>
	`);
}

function renderTable(data) {
	const tbody = $('.table tbody');
	tbody.empty();

	data.forEach(item => {
		const row = `
			<tr>
				<td>${item.projectName}</td>
				<td>${item.applicantUnitName}</td>
				<td>${item.appliedProjectName}</td>
				<td>${item.appliedTypeName}</td>
				<td>${item.participantTypeName}</td>
				<td>${item.applicationPeriodName}</td>
				<td>${item.applicationDate ? formatDate(item.applicationDate) : '-'}</td>
				<td>${item.applicationStateName}</td>
				<td>${item.stateDate ? formatDate(item.stateDate) : '-'}</td>
				<td>${item.grantAmount ?? '-'}</td>
				<td>
					<div class="dropdown">
						<button type="button" class="btn p-0 dropdown-toggle hide-arrow" data-bs-toggle="dropdown">
							<i class="bx bx-dots-vertical-rounded"></i>
						</button>
						<div class="dropdown-menu">
							<a class="dropdown-item" href="#" onclick="editItem(${item.id})">
								<i class="bx bx-edit-alt me-2"></i> Edit
							</a>
							<a class="dropdown-item" href="javascript:void(0);" onclick="deleteItem(${item.id})">
								<i class="bx bx-trash me-2"></i> Delete
							</a>
						</div>
					</div>
				</td>
			</tr>`;
		tbody.append(row);
	});
}

function exportToExcel() {
	const formData = $('#filterForm').serialize();
	window.location.href = `/Home/ExportToExcel?${formData}`;
}

function formatDate(dateString) {
	const date = new Date(dateString);
	return date.toLocaleDateString('tr-TR');
}

function addNewTip() {
	const type = $('#typeSelectAdd').val();
	const subtype = $('#subtypeInput').val();

	if (!type || !subtype) {
		alert("Lütfen tüm alanları doldurun.");
		return;
	}

	$.ajax({
		url: '/Home/AddNewTip',
		type: 'POST',
		contentType: 'application/json',
		data: JSON.stringify({ Type: type, Subtype: subtype }),
		success: function (response) {
			if (response.success) {
				alert("Başarıyla eklendi.");
				$('#subtypeInput').val('');
				loadExistingSubtypes(type);
			} else {
				alert("Hata: " + response.message);
			}
		},
		error: function () {
			alert("Sunucu hatası.");
		}
	});
}

function loadDistinctTypes() {
	$.ajax({
		url: '/Home/GetDistinctTypes',
		type: 'GET',
		success: function (data) {
			const $addSelect = $('#typeSelectAdd');
			const $deleteSelect = $('#typeSelectDelete');

			$addSelect.empty().append('<option value="">Seçiniz</option>');
			$deleteSelect.empty().append('<option value="">Seçiniz</option>');

			data.forEach(type => {
				$('<option>', { value: type, text: type }).appendTo($addSelect);
				$('<option>', { value: type, text: type }).appendTo($deleteSelect);
			});
		},
		error: function () {
			alert("Başlıkları yüklerken hata oluştu.");
		}
	});
}

$('#typeSelectDelete').change(function () {
	const type = $(this).val();
	if (type) {
		loadExistingSubtypes(type);
	}
});

function deleteSelectedTip() {
	const id = $('#existingSubtypeSelect').val();
	const type = $('#typeSelectDelete').val();

	if (!type || !id) {
		alert("Lütfen başlık ve silmek istediğiniz değeri seçin.");
		return;
	}

	if (!confirm("Seçilen değeri silmek istediğinize emin misiniz?")) return;

	$.ajax({
		url: '/Home/DeleteTip',
		type: 'POST',
		contentType: 'application/json',
		data: JSON.stringify({ id: parseInt(id) }),
		success: function (response) {
			if (response.success) {
				alert("Başarıyla silindi.");
				loadExistingSubtypes(type);
			} else {
				alert("Hata: " + response.message);
			}
		},
		error: function () {
			alert("Sunucu hatası.");
		}
	});
}

function loadExistingSubtypes(type) {
	if (!type) {
		console.error("Tip seçilmemiş.");
		return;
	}

	$.ajax({
		url: '/Home/GetTipsByType?type=' + encodeURIComponent(type),
		type: 'GET',
		success: function (data) {
			const $select = $('#existingSubtypeSelect');
			$select.empty();

			if (!data || data.length === 0) {
				$select.append('<option value="">Hiç değer yok</option>');
				return;
			}

			$select.append('<option value="">Seçiniz</option>');
			data.forEach(tip => {
				const id = tip.Id ?? tip.id;
				const text = tip.Subtype ?? tip.subtype;
				$select.append(`<option value="${id}">${text}</option>`);
			});
		},
		error: function () {
			alert("Mevcut değerleri yüklerken hata oluştu.");
		}
	});
}


//function loadExistingSubtypes(type) {
//	$.ajax({
//		url: '/Home/GetTipsByType?type=' + encodeURIComponent(type),
//		type: 'GET',
//		success: function (data) {
//			const $select = $('#existingSubtypeSelect');
//			$select.empty();

//			if (!data || data.length === 0) {
//				$select.append('<option value="">Hiç değer yok</option>');
//				return;
//			}

//			$select.append('<option value="">Seçiniz</option>');
//			data.forEach(tip => {
//				$('<option>', { value: tip.Id, text: tip.Subtype }).appendTo($select);
//			});
//		},
//		error: function () {
//			alert("Mevcut değerleri yüklerken hata oluştu.");
//		}
//	});
//}

function editItem(id) {
	$.get('/Home/GetApplicationById', { id: id }, function (html) {
		$('#modalContent').html(html);
		$('#editModal').modal('show');
	});
}

function updateApplication() {
	const model = {
		Id: $('#editId').val(),
		projectName: $('#editProjectName').val(),
		applicantUnit: $('#editApplicantUnit').val(),
		appliedProject: $('#editAppliedProject').val(),
		appliedType: $('#editAppliedType').val(),
		participantType: $('#editParticipantType').val(),
		applicationPeriod: $('#editApplicationPeriod').val(),
		applicationDate: $('#editApplicationDate').val(),
		applicationState: $('#editApplicationState').val(),
		stateDate: $('#editStateDate').val(),
		grantAmount: $('#editGrantAmount').val()
	};

	$.ajax({
		url: '/Home/UpdateApplication',
		type: 'POST',
		contentType: 'application/json',
		data: JSON.stringify(model),
		success: function (res) {
			if (res.success) {
				$('#editModal').modal('hide');
				getApplicationsPaged(currentPage);
				alert("Başarıyla güncellendi.");
			} else {
				alert("Hata oluştu.");
			}
		},
		error: function () {
			alert("Sunucu hatası.");
		}
	});
}


function deleteItem(id) {
	if (!confirm("Seçili kaydı silmek istediğinize emin misiniz?")) return;

	$.ajax({
		url: '/Home/DeleteApplication',
		type: 'POST',
		contentType: 'application/json',
		data: JSON.stringify({ id: id }),
		success: function (res) {
			if (res.success) {
				alert("Başarıyla silindi.");
				getApplicationsPaged(currentPage); // tabloyu güncelle
			} else {
				alert("Hata: " + (res.message || "Silme işlemi başarısız."));
			}
		},
		error: function () {
			alert("Sunucu hatası.");
		}
	});
}
