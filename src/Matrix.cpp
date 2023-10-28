#include "../include/Matrix.h"
constexpr auto EPSILON = 0.0001;

Matrix::Matrix() noexcept {
	this->a = 1.0;
	this->b = 0.0;
	this->c = 0.0;
	this->d = 1.0;
	this->tx = 0.0;
	this->ty = 0.0;
}
Matrix::Matrix(pugi::xml_node& matrixNode, pugi::xml_node& parentNode) noexcept {
	this->root = matrixNode;
	this->parent = parentNode;
	// empty values for a and d is 1.0; b, c, tx, and ty is 0.0
	this->a = matrixNode.attribute("a").as_double(1.0);
	this->b = matrixNode.attribute("b").as_double();
	this->c = matrixNode.attribute("c").as_double();
	this->d = matrixNode.attribute("d").as_double(1.0);
	this->tx = matrixNode.attribute("tx").as_double();
	this->ty = matrixNode.attribute("ty").as_double();
}
Matrix::Matrix(const pugi::xml_node& matrixNode, const pugi::xml_node& parentNode) noexcept {
	this->root = matrixNode;
	this->parent = parentNode;
	// empty values for a and d is 1.0; b, c, tx, and ty is 0.0
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
void Matrix::setVal(const char* name, double value, double defaultValue) noexcept {
	const bool isDefaultValue = std::abs(value - defaultValue) < EPSILON;
	if (this->root == nullptr) {
		if(!isDefaultValue) this->root = this->parent.append_child("matrix").append_child("Matrix");
		else return;
	}
	if (isDefaultValue) this->root.remove_attribute(name);
	else {
		if (this->root.attribute(name).empty()) this->root.append_attribute(name);
		this->root.attribute(name).set_value(value);
	}
}
void Matrix::removeDefaultMatrix() noexcept {
	if(std::abs(this->tx) < EPSILON && std::abs(this->ty) < EPSILON && std::abs(this->a - 1.0) < EPSILON && std::abs(this->b) < EPSILON && std::abs(this->c) < EPSILON && std::abs(this->d - 1.0) < EPSILON) {
		this->parent.remove_child("matrix");
		this->root = static_cast<pugi::xml_node>(nullptr);
	};
}
double Matrix::getA() const noexcept {
	return this->a;
}
void Matrix::setA(double a) noexcept {
	this->setVal("a", a, 1.0);
	this->a = a;
	this->removeDefaultMatrix();
}
double Matrix::getB() const noexcept {
	return this->b;
}
void Matrix::setB(double b) noexcept {
	this->setVal("b", b);
	this->b = b;
	this->removeDefaultMatrix();
}
double Matrix::getC() const noexcept {
	return this->c;
}
void Matrix::setC(double c) noexcept {
	this->setVal("c", c);
	this->c = c;
	this->removeDefaultMatrix();
}
double Matrix::getD() const noexcept {
	return this->d;
}
void Matrix::setD(double d) noexcept {
	this->setVal("d", d, 1.0);
	this->d = d;
	this->removeDefaultMatrix();
}
double Matrix::getTx() const noexcept {
	return this->tx;
}
void Matrix::setTx(double tx) noexcept {
	this->setVal("tx", tx);
	this->tx = tx;
	this->removeDefaultMatrix();
}
double Matrix::getTy() const noexcept {
	return this->ty;
}
void Matrix::setTy(double ty) noexcept {
	this->setVal("ty", ty);
	this->ty = ty;
	this->removeDefaultMatrix();
}
pugi::xml_node& Matrix::getRoot() noexcept {
	return this->root;
}
const pugi::xml_node& Matrix::getRoot() const noexcept {
	return this->root;
}
