#include "../include/Matrix.h"
#include <limits>
Matrix::Matrix(pugi::xml_node& matrixNode) noexcept {
	this->root = matrixNode;
	// empty values for a and d is 1.0; b, c, tx, and ty is 0.0
	this->a = matrixNode.attribute("a").as_double(1.0);
	this->b = matrixNode.attribute("b").as_double();
	this->c = matrixNode.attribute("c").as_double();
	this->d = matrixNode.attribute("d").as_double(1.0);
	this->tx = matrixNode.attribute("tx").as_double();
	this->ty = matrixNode.attribute("ty").as_double();
}
Matrix::Matrix(const pugi::xml_node& matrixNode) noexcept {
	this->root = matrixNode;
	this->a = matrixNode.attribute("a").as_double(1.0);
	this->b = matrixNode.attribute("b").as_double();
	this->c = matrixNode.attribute("c").as_double();
	this->d = matrixNode.attribute("d").as_double(1.0);
	this->tx = matrixNode.attribute("tx").as_double();
	this->ty = matrixNode.attribute("ty").as_double();
}
Matrix::~Matrix() noexcept {

}
// responsibility of the caller to move this matrix's root somewhere else
Matrix::Matrix(const Matrix& matrix) noexcept {
	auto parent = matrix.root.parent();
	this->root = parent.insert_copy_after(matrix.root, matrix.root);
	this->setA(matrix.getA());
	this->setB(matrix.getB());
	this->setC(matrix.getC());
	this->setD(matrix.getD());
	this->setTx(matrix.getTx());
	this->setTy(matrix.getTy());
}
double Matrix::getA() const noexcept {
	return this->a;
}
void Matrix::setA(double a) noexcept {
	if (std::abs(a - 1.0) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("a");
	else {
		if (this->root.attribute("a").empty()) this->root.append_attribute("a");
		this->root.attribute("a").set_value(a);
	}
	this->a = a;
}
double Matrix::getB() const noexcept {
	return this->b;
}
void Matrix::setB(double b) noexcept {
	if (std::abs(b) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("b");
	else {
		if (this->root.attribute("b").empty()) this->root.append_attribute("b");
		this->root.attribute("b").set_value(b);
	}
	this->b = b;
}
double Matrix::getC() const noexcept {
	return this->c;
}
void Matrix::setC(double c) noexcept {
	if (std::abs(c) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("c");
	else {
		if (this->root.attribute("c").empty()) this->root.append_attribute("c");
		this->root.attribute("c").set_value(c);
	}
	this->c = c;
}
double Matrix::getD() const noexcept {
	return this->d;
}
void Matrix::setD(double d) noexcept {
	if (std::abs(d - 1.0) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("d");
	else {
		if (this->root.attribute("d").empty()) this->root.append_attribute("d");
		this->root.attribute("d").set_value(d);
	}
	this->d = d;
}
double Matrix::getTx() const noexcept {
	return this->tx;
}
void Matrix::setTx(double tx) noexcept {
	if (std::abs(tx) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("tx");
	else {
		if (this->root.attribute("tx").empty()) this->root.append_attribute("tx");
		this->root.attribute("tx").set_value(tx);
	}
	this->tx = tx;
}
double Matrix::getTy() const noexcept {
	return this->ty;
}
void Matrix::setTy(double ty) noexcept {
	if (std::abs(ty) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("ty");
	else {
		if (this->root.attribute("ty").empty()) this->root.append_attribute("ty");
		this->root.attribute("ty").set_value(ty);
	}
	this->ty = ty;
}
pugi::xml_node& Matrix::getRoot() noexcept {
	return this->root;
}
const pugi::xml_node& Matrix::getRoot() const noexcept {
	return this->root;
}
