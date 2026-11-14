
const { test, expect } = require('@playwright/test');

test('ST-03 - Lap ke hoach co canh bao va chap nhan', async ({ page }) => {

  // Cho phép bài kiểm thử chạy tối đa 180 giây
  test.setTimeout(180000);

  // Mỗi lần chạy tạo một kế hoạch riêng, không trùng dữ liệu
  const tenKeHoach = `ST03-DaLat-${Date.now()}`;

  // Ghi kết quả từng bước ra Terminal
  function ghiKetQua(noiDung) {
    console.log(`[ST-03] ${noiDung}`);
  }

  // =====================================================
  // HÀM DÙNG CHUNG: THÊM HOẠT ĐỘNG
  // =====================================================

  async function themHoatDong({
    loai,
    noiDung,
    batDau,
    ketThuc,
    diaDiem,
    chiPhi,
    ghiChu
  }) {

    // Chọn dịch vụ hoặc địa điểm tham khảo nếu có
    if (loai) {
      await page.getByTestId('activity-form-0')
        .getByLabel('Chọn mục tham khảo hoặc tự nh')
        .selectOption(loai);
    } else {
      // Đưa về chế độ tự nhập để tránh giữ lựa chọn cũ
      const chonLoai = page.getByTestId('activity-form-0')
        .getByLabel('Chọn mục tham khảo hoặc tự nh');

      // Chỉ chọn lại nếu có tùy chọn tự nhập
      const tuNhap = chonLoai.locator('option')
        .filter({ hasText: /tự nhập/i });

      if (await tuNhap.count() > 0) {
        const giaTri = await tuNhap.first().getAttribute('value');
        if (giaTri !== null) {
          await chonLoai.selectOption(giaTri);
        }
      }
    }

    await page.getByRole('textbox', {
      name: 'Nội dung hoạt động'
    }).fill(noiDung);

    await page.getByRole('textbox', {
      name: 'Bắt đầu'
    }).fill(batDau);

    await page.getByRole('textbox', {
      name: 'Kết thúc'
    }).fill(ketThuc);

    await page.getByRole('textbox', {
      name: 'Địa điểm / địa chỉ'
    }).fill(diaDiem);

    await page.getByRole('spinbutton', {
      name: 'Chi phí dự kiến (đ)'
    }).fill(String(chiPhi));

    await page.getByRole('textbox', {
      name: 'Ghi chú'
    }).fill(ghiChu);

    await page.getByRole('button', {
      name: 'Thêm vào lịch trình'
    }).click();

    await page.getByRole('button', {
      name: 'Có, xác nhận'
    }).click();

    ghiKetQua(`Đã thực hiện thêm: ${noiDung}`);
  }

  // =====================================================
  // BƯỚC 1: TẠO KẾ HOẠCH NHÁP
  // =====================================================

  await test.step('01 - Tao ke hoach nhap', async () => {

    ghiKetQua('BƯỚC 1: Tạo kế hoạch mới');

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

    // Nếu quay về danh sách, mở đúng kế hoạch vừa tạo
    if (await page.getByRole('textbox', {
      name: 'Nội dung hoạt động'
    }).count() === 0) {

      await page.getByRole('link', {
        name: tenKeHoach
      }).click();
    }

    // Xác nhận đã mở trang có thể thêm hoạt động
    await expect(
      page.getByRole('textbox', {
        name: 'Nội dung hoạt động'
      })
    ).toBeVisible();

    ghiKetQua(`PASS bước 1: Tạo kế hoạch ${tenKeHoach}`);
  });

  // =====================================================
  // BƯỚC 2: THÊM HOẠT ĐỘNG 1
  // =====================================================

  await test.step('02 - Them hoat dong an sang', async () => {

    ghiKetQua('BƯỚC 2: Thêm hoạt động Ăn sáng chợ');

    await themHoatDong({
      noiDung: 'Ăn sáng chợ',
      batDau: '2026-12-05T08:00',
      ketThuc: '2026-12-05T09:00',
      diaDiem: 'Chợ Đà Lạt',
      chiPhi: 100000,
      ghiChu: 'Ăn sáng trước khi tham quan'
    });

    await expect(
      page.getByTestId('trip-total')
    ).toContainText(/100[\s.,]*000/);

    ghiKetQua('PASS bước 2: Tổng chi phí 100.000đ');
  });

  // =====================================================
  // BƯỚC 3: THÊM HOẠT ĐỘNG 2
  // =====================================================

  await test.step('03 - Them dich vu DV01', async () => {

    ghiKetQua('BƯỚC 3: Thêm DV01, trùng giờ hoạt động 1');

    await themHoatDong({
      loai: 'Service:5',
      noiDung: 'DV01 - Phòng đôi view đồi',
      batDau: '2026-12-05T08:00',
      ketThuc: '2026-12-05T09:00',
      diaDiem: 'Đà Lạt',
      chiPhi: 1600000,
      ghiChu: 'Dịch vụ lưu trú Đà Lạt'
    });

    await expect(
      page.getByTestId('trip-total')
    ).toContainText(/1[\s.,]*700[\s.,]*000/);

    ghiKetQua('PASS bước 3: Tổng chi phí 1.700.000đ');
  });

  // =====================================================
  // BƯỚC 4: THÊM HOẠT ĐỘNG 3
  // =====================================================

  await test.step('04 - Them dia diem DD01', async () => {

    ghiKetQua('BƯỚC 4: Thêm Hồ Xuân Hương');

    await themHoatDong({
      loai: 'Place:1',
      noiDung: 'DD01-Hồ Xuân Hương',
      batDau: '2026-12-06T09:00',
      ketThuc: '2026-12-06T11:00',
      diaDiem: 'Hồ Xuân Hương',
      chiPhi: 500000,
      ghiChu: 'Tham quan Hồ Xuân Hương'
    });

    await expect(
      page.getByTestId('trip-total')
    ).toContainText(/2[\s.,]*200[\s.,]*000/);

    ghiKetQua('PASS bước 4: Tổng chi phí 2.200.000đ');
  });

  // =====================================================
  // BƯỚC 5: KIỂM TRA CẢNH BÁO
  // =====================================================

  await test.step('05 - Kiem tra canh bao', async () => {

    ghiKetQua('BƯỚC 5: Kiểm tra trùng giờ và vượt ngân sách');

    const canhBao = page.getByTestId('trip-warnings');

    await expect(canhBao).toBeVisible();

    const noiDung = await canhBao.innerText();

    ghiKetQua(`Cảnh báo thực tế: ${noiDung}`);

    // Kiểm tra cảnh báo trùng giờ
    expect(
      noiDung,
      'Thiếu cảnh báo trùng thời gian'
    ).toMatch(/trùng|chồng|giao nhau/i);

    // Kiểm tra cảnh báo vượt ngân sách
    expect(
      noiDung,
      'Thiếu cảnh báo vượt ngân sách'
    ).toMatch(/vượt|ngân sách/i);

    // Kiểm tra tổng vẫn là 2.200.000đ
    await expect(
      page.getByTestId('trip-total')
    ).toContainText(/2[\s.,]*200[\s.,]*000/);

    ghiKetQua('PASS bước 5: Có cả hai loại cảnh báo');
  });

  // =====================================================
  // BƯỚC 6: CHẤP NHẬN KẾ HOẠCH
  // =====================================================

  await test.step('06 - Chap nhan ke hoach', async () => {

    ghiKetQua('BƯỚC 6: Chấp nhận kế hoạch dù có cảnh báo');

    await page.getByRole('button', {
      name: 'Chấp nhận kế hoạch'
    }).click();

    await page.getByRole('button', {
      name: 'Có, xác nhận'
    }).click();

    // Kiểm tra trạng thái được hiển thị trên giao diện
    await expect(
      page.getByText('Đã chấp nhận', {
        exact: true
      }).first()
    ).toBeVisible();

    // Cảnh báo phải còn hiển thị
    const canhBao = page.getByTestId('trip-warnings');

    await expect(canhBao).toBeVisible();

    const noiDung = await canhBao.innerText();

    expect(noiDung).toMatch(/trùng|chồng|giao nhau/i);
    expect(noiDung).toMatch(/vượt|ngân sách/i);

    ghiKetQua('PASS bước 6: Đã chấp nhận, cảnh báo vẫn còn');
  });

  // =====================================================
  // BƯỚC 7: THÊM HOẠT ĐỘNG THỨ TƯ
  // =====================================================

  await test.step('07 - Them hoat dong ngay 07-12', async () => {

    ghiKetQua('BƯỚC 7: Thêm hoạt động sau khi chấp nhận');

    await themHoatDong({
      noiDung: 'Tham quan trung tâm Đà Lạt',
      batDau: '2026-12-07T09:00',
      ketThuc: '2026-12-07T10:00',
      diaDiem: 'Đà Lạt',
      chiPhi: 0,
      ghiChu: 'Tham quan tự do'
    });

    // Kế hoạch phải tự chuyển về Nháp
    await expect(
      page.getByText('Nháp', {
        exact: true
      }).first()
    ).toBeVisible();

    // Chi phí không đổi vì hoạt động mới có chi phí 0
    await expect(
      page.getByTestId('trip-total')
    ).toContainText(/2[\s.,]*200[\s.,]*000/);

    ghiKetQua('PASS bước 7: Kế hoạch trở về Nháp');
  });

  // =====================================================
  // BƯỚC 8: LƯU NHÁP
  // =====================================================

  await test.step('08 - Luu nhap', async () => {

    ghiKetQua('BƯỚC 8: Lưu kế hoạch nháp');

    await page.getByRole('button', {
      name: 'Lưu nháp'
    }).click();

    await page.getByRole('button', {
      name: 'Có, xác nhận'
    }).click();

    // Kiểm tra thông báo lưu nháp thành công
    await expect(
      page.getByText(/Đã lưu nháp/i).first()
    ).toBeVisible();

    ghiKetQua('PASS bước 8: Đã lưu nháp');
  });

  // =====================================================
  // KẾT THÚC
  // =====================================================

  console.log('');
  console.log('======================================');
  console.log('ST-03: HOÀN THÀNH KIỂM THỬ');
  console.log(`Kế hoạch: ${tenKeHoach}`);
  console.log('======================================');
});
