import React from "react";
import { Modal } from "antd";
import "../pages/login/LoginPage.css";

interface CustomModalProps {
  open: boolean;
  onCancel: () => void;
  title?: React.ReactNode;
  children: React.ReactNode;
  footer?: React.ReactNode;
  width?: number;
  wrapClassName?: string;
}

const CustomModal: React.FC<CustomModalProps> = ({
  open,
  onCancel,
  title,
  children,
  footer,
  width,
  wrapClassName,
}) => (
  <Modal
    open={open}
    onCancel={onCancel}
    title={title}
    footer={footer}
    width={width || 420}
    wrapClassName={`login-modal-wrap ${wrapClassName ?? ""}`.trim()}
    className="login-modal"
    centered
    destroyOnHidden
    maskClosable={!footer}
  >
    {children}
  </Modal>
);

export default CustomModal;
