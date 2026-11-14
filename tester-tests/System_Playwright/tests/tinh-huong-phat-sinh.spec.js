
const { test, expect } = require('@playwright/test');

test('ST-08 - Ghi nhan va xu ly tinh huong phat sinh', async ({ page }) => {
  test.setTimeout(180000);

  // Mỗi lần chạy sử dụng một kế hoạch riêng
  const tenKeHoach = `ST08-DaLat-${Date.now()}`;

  const moTa = 'Mưa lớn, khu tham quan đóng cửa';
  const phuongAn1 = 'Chuyển sang bảo tàng';
  const phuongAn2 = 'Đổi sang ngày 07/12';
  const ketQua = 'Đã dời sang 07/12';

  function ghi(noiDung) {
    console.log(`[ST-08] ${noiDung}`);
  }

  // Xử lý hộp thoại xác nhận khi xuất hiện
  async function xacNhanNeuCo() {
    const nut = page.getByRole('button', {
      name: 'Có, xác nhận'
    });

    try {
      await nut.waitFor({
        state: 'visible',
        timeout: 1500
      });
      await nut.click();
    } catch {
      // Không có hộp thoại xác nhận
    }
  }

  // ==================================================
  // BƯỚC 1. TẠO KẾ HOẠCH THỬ NGHIỆM
  // ==================================================

  await test.step('01 - Tao ke hoach rieng', async () => {
    ghi('BƯỚC 1: Tạo kế hoạch mới');

    await page.goto('http://localhost:5080/');

    await page.getByRole('link', {
      name: 'Đăng nhập',
      exact: true
    }).click();

    await page.getByRole('button', {
      name: 'Khách du lịch Lên lịch trình'
    }).click();

    await page.getByRole('link', {
      name: 'Tạo kế hoạch'
    }).click();

    await page.getByRole('textbox', {
      name: 'Tên chuyến đi'
    }).fill(tenKeHoach);

    await page.getByRole('textbox', {
      name: 'Điểm đến'
    }).fill('Đà Lạt');

    await page.getByRole('textbox', {
      name: 'Ngày bắt đầu'
    }).fill('2026-12-05');

    await page.getByRole('textbox', {
      name: 'Ngày kết thúc'
    }).fill('2026-12-08');

    await page.getByRole('spinbutton', {
      name: 'Ngân sách dự kiến (đ)'
    }).fill('2000000');

    await page.getByRole('button', {
      name: 'Tạo kế hoạch nháp'
    }).click();

    await page.getByRole('button', {
      name: 'Có, xác nhận'
    }).click();

    // Trường hợp hệ thống quay về danh sách
    const oHoatDong = page.getByRole('textbox', {
      name: 'Nội dung hoạt động'
    });

    if (await oHoatDong.count() === 0) {
      await page.getByRole('link', {
        name: tenKeHoach
      }).click();
    }

    await expect(oHoatDong).toBeVisible();

    ghi(`Đã tạo kế hoạch: ${tenKeHoach}`);
  });

  // ==================================================
  // BƯỚC 2. TẠO HOẠT ĐỘNG HỒ XUÂN HƯƠNG
  // ==================================================

  await test.step('02 - Tao hoat dong bi anh huong', async () => {
    ghi('BƯỚC 2: Tạo hoạt động Hồ Xuân Hương');

    await page.getByRole('textbox', {
      name: 'Nội dung hoạt động'
    }).fill('Tham quan Hồ Xuân Hương');

    await page.getByRole('textbox', {
      name: 'Bắt đầu'
    }).fill('2026-12-05T09:00');

    await page.getByRole('textbox', {
      name: 'Kết thúc'
    }).fill('2026-12-05T11:00');

    await page.getByRole('textbox', {
      name: 'Địa điểm / địa chỉ'
    }).fill('Hồ Xuân Hương');

    await page.getByRole('spinbutton', {
      name: 'Chi phí dự kiến (đ)'
    }).fill('0');

    await page.getByRole('textbox', {
      name: 'Ghi chú'
    }).fill('Tham quan Hồ Xuân Hương theo lịch trình');

    await page.getByRole('button', {
      name: 'Thêm vào lịch trình'
    }).click();

    await page.getByRole('button', {
      name: 'Có, xác nhận'
    }).click();

    await expect(
      page.getByText('Tham quan Hồ Xuân Hương').first()
    ).toBeVisible();

    ghi('PASS: Đã tạo hoạt động ngày 05/12');
  });

  // ==================================================
  // BƯỚC 3. MỞ BIỂU MẪU TÌNH HUỐNG
  // ==================================================

  await test.step('03 - Mo bieu mau tinh huong', async () => {
    ghi('BƯỚC 3: Mở ghi nhận tình huống');

    await page.getByRole('link', {
      name: /Tình huống của chuyến đi/
    }).click();

    // Kiểm tra biểu mẫu ghi nhận đã xuất hiện
    await expect(
      page.getByRole('textbox', {
        name: 'Mô tả',
        exact: true
      })
    ).toBeVisible();

    // Chọn chuyến đi vừa tạo nếu biểu mẫu chưa chọn đúng
   
    // Chọn chuyến đi trong biểu mẫu ghi nhận tình huống
    // Dùng đúng ID của ô nhập, tránh nhầm ô lọc danh sách
    const chonChuyen = page.locator('#incident-trip');

    await expect(chonChuyen).toBeVisible();

    await chonChuyen.selectOption({
      label: tenKeHoach
    });

    // Xác nhận đã chọn đúng kế hoạch vừa tạo
    await expect(
  chonChuyen.locator('option:checked')
).toHaveText(tenKeHoach);
    

    ghi(`Đã chọn chuyến đi: ${tenKeHoach}`);


    await page.getByRole('combobox', {
      name: 'Loại tình huống'
    }).selectOption({
      label: 'Thời tiết'
    });

    await page.getByRole('textbox', {
      name: 'Vị trí'
    }).fill('Hồ Xuân Hương');

    // Tìm danh sách hoạt động ảnh hưởng
    const chonHoatDong = page.locator('select').filter({
      has: page.locator('option', {
        hasText: /Tham quan Hồ Xuân Hương/
      })
    });

    await expect(chonHoatDong).toHaveCount(1);

    const giaTriHoatDong = await chonHoatDong
      .locator('option')
      .filter({
        hasText: /Tham quan Hồ Xuân Hương/
      })
      .first()
      .getAttribute('value');

    expect(giaTriHoatDong).toBeTruthy();

    await chonHoatDong.selectOption(giaTriHoatDong);

    ghi('Đã liên kết hoạt động Hồ Xuân Hương');
  });

  // ==================================================
  // BƯỚC 4. THỬ GHI NHẬN THIẾU MÔ TẢ
  // ==================================================

  await test.step('04 - Kiem tra thieu mo ta', async () => {
    ghi('BƯỚC 4: Thử lưu khi mô tả trống');

    const oMoTa = page.getByRole('textbox', {
      name: 'Mô tả',
      exact: true
    });

    await oMoTa.fill('');

    await page.getByRole('button', {
      name: 'Ghi nhận Chưa giải quyết'
    }).click();

    // Có thể bị chặn bằng HTML validation hoặc thông báo
    const biChanTaiTrinhDuyet = await oMoTa.evaluate(
      element => !element.validity.valid
    );

    const coThongBao = await page.getByText(
      /vui lòng.*mô tả|mô tả.*bắt buộc/i
    ).count() > 0;

    expect(
      biChanTaiTrinhDuyet || coThongBao,
      'Biểu mẫu chưa thể hiện lỗi thiếu mô tả'
    ).toBeTruthy();

    ghi('PASS: Hệ thống chặn mô tả trống');
  });

  // ==================================================
  // BƯỚC 5. GHI NHẬN TÌNH HUỐNG HỢP LỆ
  // ==================================================

  await test.step('05 - Ghi nhan tinh huong hop le', async () => {
    ghi('BƯỚC 5: Tạo tình huống thời tiết');

    await page.getByRole('textbox', {
      name: 'Mô tả',
      exact: true
    }).fill(moTa);

    await page.getByRole('button', {
      name: 'Ghi nhận Chưa giải quyết'
    }).click();

    await page.getByRole('button', {
      name: 'Có, xác nhận'
    }).click();

    // Sau khi tạo phải mở được phần ghi phương án
    await expect(
      page.getByRole('textbox', {
        name: 'Ghi phương án mới'
      })
    ).toBeVisible();

    await expect(
      page.getByText('Chưa giải quyết', {
        exact: true
      }).first()
    ).toBeVisible();

    ghi('PASS: Đã ghi nhận tình huống chưa giải quyết');
  });

  // ==================================================
  // BƯỚC 6. GHI PHƯƠNG ÁN THỨ NHẤT
  // ==================================================

  await test.step('06 - Ghi phuong an 1', async () => {
    ghi('BƯỚC 6: Chuyển sang bảo tàng');

    await page.getByRole('textbox', {
      name: 'Ghi phương án mới'
    }).fill(phuongAn1);

    await page.getByRole('button', {
      name: 'Lưu phương án'
    }).click();

    await page.getByRole('button', {
      name: 'Có, xác nhận'
    }).click();

    await expect(
      page.getByText(phuongAn1, {
        exact: true
      })
    ).toBeVisible();

    ghi('PASS: Phương án 1 đã được lưu');
  });

  // ==================================================
  // BƯỚC 7. GHI PHƯƠNG ÁN THỨ HAI
  // ==================================================

  await test.step('07 - Ghi phuong an 2', async () => {
    ghi('BƯỚC 7: Đổi sang ngày 07/12');

    await page.getByRole('textbox', {
      name: 'Ghi phương án mới'
    }).fill(phuongAn2);

    await page.getByRole('button', {
      name: 'Lưu phương án'
    }).click();

    await page.getByRole('button', {
      name: 'Có, xác nhận'
    }).click();

    await expect(
      page.getByText(phuongAn1, {
        exact: true
      })
    ).toBeVisible();

    await expect(
      page.getByText(phuongAn2, {
        exact: true
      })
    ).toBeVisible();

    // Đối chiếu lịch sử hai phương án
    const hienHanh = page.getByText(phuongAn2, {
      exact: true
    });

    await expect(hienHanh).toBeVisible();

    ghi('PASS: Cả hai phương án có trong lịch sử');
  });

  // ==================================================
  // BƯỚC 8. THỬ GHI KẾT QUẢ TRỐNG
  // ==================================================

  await test.step('08 - Kiem tra ket qua trong', async () => {
    ghi('BƯỚC 8: Thử giải quyết với mô tả kết quả trống');

    await page.getByLabel(
      'Mức độ giải quyếtĐã giải quyế'
    ).selectOption('Đã giải quyết');

    await page.getByRole('textbox', {
      name: 'Mô tả kết quả'
    }).fill('');

    await page.getByRole('button', {
      name: 'Lưu kết quả thực tế'
    }).click();

    await xacNhanNeuCo();

    await expect(
      page.getByText(
        /Vui lòng mô tả kết quả thực tế/i
      ).first()
    ).toBeVisible();

    ghi('PASS: Hệ thống từ chối kết quả trống');
  });

  // ==================================================
  // BƯỚC 9. GHI KẾT QUẢ HỢP LỆ
  // ==================================================

  await test.step('09 - Ghi ket qua da giai quyet', async () => {
    ghi('BƯỚC 9: Ghi kết quả thực tế');

    await page.getByRole('textbox', {
      name: 'Mô tả kết quả'
    }).fill(ketQua);

    await page.getByRole('button', {
      name: 'Lưu kết quả thực tế'
    }).click();

    await page.getByRole('button', {
      name: 'Có, xác nhận'
    }).click();

    await expect(
      page.getByText('Đã giải quyết', {
        exact: true
      }).first()
    ).toBeVisible();

    await expect(
      page.getByText(ketQua, {
        exact: true
      }).first()
    ).toBeVisible();

    ghi('PASS: Tình huống chuyển sang Đã giải quyết');
  });

  // ==================================================
  // BƯỚC 10. THỬ CẬP NHẬT SAU KHI KẾT THÚC
  // ==================================================

  
await test.step('10 - Kiem tra khoa cap nhat', async () => {

  console.log('BUOC 10: Kiem tra tinh huong da ket thuc');

  // Kiểm tra trạng thái đã giải quyết
  await expect(
    page.getByText('Đã giải quyết', {
      exact: true
    }).first()
  ).toBeVisible();

  const oPhuongAn = page.getByRole('textbox', {
    name: 'Ghi phương án mới'
  });

  const nutLuu = page.getByRole('button', {
    name: 'Lưu phương án'
  });

  // Ô nhập phải bị ẩn, bị khóa hoặc không tồn tại
  const khongNhapDuoc =
    await oPhuongAn.count() === 0 ||
    !(await oPhuongAn.isVisible()) ||
    await oPhuongAn.isDisabled();

  // Nút lưu phải bị ẩn, bị khóa hoặc không tồn tại
  const khongLuuDuoc =
    await nutLuu.count() === 0 ||
    !(await nutLuu.isVisible()) ||
    await nutLuu.isDisabled();

  expect(
    khongNhapDuoc && khongLuuDuoc,
    'Tinh huong da giai quyet nhung van cho them phuong an'
  ).toBe(true);

  console.log('PASS: Khong the them phuong an sau khi giai quyet');
});


  console.log('');
  console.log('===================================');
  console.log('ST-08: DA HOAN THANH CAC BUOC TEST');
  console.log('Ke hoach:', tenKeHoach);
  console.log('===================================');
});
