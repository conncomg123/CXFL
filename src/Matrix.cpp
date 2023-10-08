#include "../include/Matrix.h"
Matrix::Matrix(pugi::xml_node& matrixNode) {
	this->root = matrixNode;
	// empty values for a and d is 1.0; b, c, tx, and ty is 0.0
	this->a  = matrixNode.attribute("a").as_double(1.0);
	this->b  = matrixNode.attribute("b").as_double();
	this->c  = matrixNode.attribute("c").as_double();
	this->d  = matrixNode.attribute("d").as_double(1.0);
	this->tx = matrixNode.attribute("tx").as_double();
	this->ty = matrixNode.attribute("ty").as_double();
}
Matrix::Matrix(const pugi::xml_node& matrixNode) {
	this->root = matrixNode;
	this->a  = matrixNode.attribute("a").as_double(1.0);
	this->b  = matrixNode.attribute("b").as_double();
	this->c  = matrixNode.attribute("c").as_double();
	this->d  = matrixNode.attribute("d").as_double(1.0);
	this->tx = matrixNode.attribute("tx").as_double();
	this->ty = matrixNode.attribute("ty").as_double();
}
Matrix::~Matrix() {

}
// responsibility of the caller to move this matrix's root somewhere else
Matrix::Matrix(const Matrix& matrix) {
	auto parent = matrix.root.parent();
	this->root = parent.insert_copy_after(matrix.root, matrix.root);
	this->setA(matrix.getA());
	this->setB(matrix.getB());
	this->setC(matrix.getC());
	this->setD(matrix.getD());
	this->setTx(matrix.getTx());
	this->setTy(matrix.getTy());
}
double Matrix::getA() const {
	return this->a;
}
void Matrix::setA(double a)  {
	if (std::abs(a - 1.0) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("a");
	else this->root.attribute("a").set_value(a);
	this->a = a;
}
double Matrix::getB() const {
	return this->b;
}
void Matrix::setB(double b)  {
	if (std::abs(b) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("b");
	else this->root.attribute("b").set_value(b);
	this->b = b;
}
double Matrix::getC() const {
	return this->c;
}
void Matrix::setC(double c) {
	if (std::abs(c) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("c");
	else this->root.attribute("c").set_value(c);
	this->c = c;
}
double Matrix::getD() const {
	return this->d;
}
void Matrix::setD(double d) {
	if (std::abs(d - 1.0) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("d");
	else this->root.attribute("d").set_value(d);
	this->d = d;
}
double Matrix::getTx() const {
	return this->tx;
}
void Matrix::setTx(double tx) {
	if (std::abs(tx) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("tx");
	else this->root.attribute("tx").set_value(tx);
	this->tx = tx;
}
double Matrix::getTy() const {
	return this->ty;
}
void Matrix::setTy(double ty) {
	if (std::abs(ty) < std::numeric_limits<double>::epsilon()) this->root.remove_attribute("ty");
	else this->root.attribute("ty").set_value(ty);
	this->ty = ty;
}
pugi::xml_node& Matrix::getRoot() {
	return this->root;
}