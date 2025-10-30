-- Leave Management System Schema (MySQL)

CREATE TABLE role (
  role_id INT PRIMARY KEY AUTO_INCREMENT,
  role_name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE users (
  user_id INT PRIMARY KEY AUTO_INCREMENT,
  email VARCHAR(255) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  first_name VARCHAR(100),
  last_name VARCHAR(100),
  phone VARCHAR(20),
  role_id INT NOT NULL,
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_users_role FOREIGN KEY (role_id) REFERENCES role(role_id)
);

CREATE TABLE employee (
  employee_id INT PRIMARY KEY,
  department VARCHAR(100),
  leave_balance DECIMAL(6,2) DEFAULT 0,
  position VARCHAR(100),
  CONSTRAINT fk_employee_user FOREIGN KEY (employee_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE TABLE supervisor (
  supervisor_id INT PRIMARY KEY,
  approval_limit INT DEFAULT NULL,
  CONSTRAINT fk_supervisor_user FOREIGN KEY (supervisor_id) REFERENCES users(user_id) ON DELETE CASCADE
);

CREATE TABLE leave_policy (
  policy_id INT PRIMARY KEY AUTO_INCREMENT,
  leave_type VARCHAR(50) NOT NULL UNIQUE,
  max_days INT,
  min_days INT,
  requires_document BOOLEAN DEFAULT FALSE,
  is_paid BOOLEAN DEFAULT TRUE
);

CREATE TABLE leave_application (
  application_id INT PRIMARY KEY AUTO_INCREMENT,
  employee_id INT NOT NULL,
  policy_id INT NOT NULL,
  start_date DATETIME NOT NULL,
  end_date DATETIME NOT NULL,
  days DECIMAL(6,2) NOT NULL,
  status ENUM('Pending','Approved','Rejected','Cancelled') DEFAULT 'Pending',
  submitted_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  actioned_by INT NULL,
  actioned_at DATETIME NULL,
  reason TEXT NULL,
  CONSTRAINT fk_leave_employee FOREIGN KEY (employee_id) REFERENCES employee(employee_id),
  CONSTRAINT fk_leave_policy FOREIGN KEY (policy_id) REFERENCES leave_policy(policy_id),
  CONSTRAINT fk_leave_actioned_by FOREIGN KEY (actioned_by) REFERENCES users(user_id)
);

CREATE TABLE document (
  document_id INT PRIMARY KEY AUTO_INCREMENT,
  application_id INT NOT NULL,
  uploaded_by INT NOT NULL,
  file_path VARCHAR(1024) NOT NULL,
  mime_type VARCHAR(100),
  size_bytes BIGINT,
  uploaded_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_doc_application FOREIGN KEY (application_id) REFERENCES leave_application(application_id) ON DELETE CASCADE,
  CONSTRAINT fk_doc_user FOREIGN KEY (uploaded_by) REFERENCES users(user_id)
);

CREATE TABLE audit_action (
  action_code VARCHAR(50) PRIMARY KEY,
  description VARCHAR(255) NOT NULL
);

CREATE TABLE audit_log (
  log_id INT PRIMARY KEY AUTO_INCREMENT,
  user_id INT,
  action_code VARCHAR(50),
  target_type VARCHAR(50),
  target_id VARCHAR(100),
  details TEXT,
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_audit_user FOREIGN KEY (user_id) REFERENCES users(user_id),
  CONSTRAINT fk_audit_action FOREIGN KEY (action_code) REFERENCES audit_action(action_code)
);

-- View for reports (recommended instead of a physical report table)
CREATE VIEW vw_leave_report AS
SELECT
  la.application_id,
  u.email AS employee_email,
  CONCAT(u.first_name, ' ', u.last_name) AS employee_name,
  lp.leave_type,
  la.start_date,
  la.end_date,
  la.submitted_at AS date_submitted,
  la.actioned_at AS date_actioned,
  la.days AS days_on_leave,
  la.reason AS reason_for_leave,
  la.status
FROM leave_application la
JOIN employee e ON la.employee_id = e.employee_id
JOIN users u ON e.employee_id = u.user_id
JOIN leave_policy lp ON la.policy_id = lp.policy_id;
